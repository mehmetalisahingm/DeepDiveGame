using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Session;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    [DisallowMultipleComponent]
    public sealed class LoadoutDiverBinding : MonoBehaviour
    {
        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private EconomyManager economy;
        private bool subscribed;
        private double nextSync;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<LoadoutDiverBinding>() == null)
                session.gameObject.AddComponent<LoadoutDiverBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            if (adapter == null || manager == null)
            {
                enabled = false;
                return;
            }
            EnsureEconomy();
        }

        private void OnEnable()
        {
            EnsureEconomy();
            BindPurchaseAuthority();
        }

        private void Update()
        {
            EnsureEconomy();
            BindPurchaseAuthority();
            if (economy == null || adapter == null || manager == null || !manager.IsListening || !adapter.IsAuthority)
                return;
            if (adapter.Session.State.Phase == SessionPhase.Dive) return;
            if (Time.unscaledTimeAsDouble < nextSync) return;
            nextSync = Time.unscaledTimeAsDouble + 0.5d;
            SyncAllPlayers();
        }

        private void EnsureEconomy()
        {
            var current = GetComponent<EconomyManager>();
            if (current == null && adapter != null)
                current = gameObject.AddComponent<EconomyManager>();

            if (current != null && GetComponent<EconomySaveStore>() == null)
                gameObject.AddComponent<EconomySaveStore>();
            var save = GetComponent<EconomySaveStore>();
            if (save != null) save.CanWrite = () => manager != null && manager.IsListening && adapter != null && adapter.IsAuthority;

            if (!ReferenceEquals(economy, current))
            {
                Unsubscribe();
                economy = current;
            }

            if (economy != null && !subscribed)
            {
                economy.OnLoadoutChanged += LoadoutChanged;
                subscribed = true;
            }
        }

        private void BindPurchaseAuthority()
        {
            if (economy != null && adapter != null && manager != null && manager.IsListening && adapter.IsAuthority)
                EconomyPurchaseAuthority.Bind(HandlePurchase);
            else EconomyPurchaseAuthority.Unbind(HandlePurchase);
        }

        private TransactionResult HandlePurchase(PlayerId player, string equipmentId, ulong requestId)
        {
            if (economy == null || adapter == null || manager == null || !manager.IsListening || !adapter.IsAuthority)
                return TransactionResult.Reject(requestId, "InvalidState", economy != null ? economy.Revision : 0);
            if (adapter.Session.State.Phase == SessionPhase.Dive)
                return TransactionResult.Reject(requestId, "WrongPhase", economy.Revision);
            if (!adapter.Session.Roster.ContainsKey(player) || !manager.ConnectedClients.ContainsKey(player.Value))
                return TransactionResult.Reject(requestId, "PlayerInactive", economy.Revision);
            return economy.TryPurchase(player, equipmentId, requestId);
        }

        private void LoadoutChanged(PlayerId player)
        {
            if (adapter == null || !adapter.IsAuthority || adapter.Session.State.Phase == SessionPhase.Dive) return;
            SyncPlayer(player);
        }

        private void SyncAllPlayers()
        {
            foreach (var pair in manager.ConnectedClients)
                SyncPlayer(new PlayerId(pair.Key));
        }

        private void SyncPlayer(PlayerId player)
        {
            if (economy == null || manager == null ||
                !manager.ConnectedClients.TryGetValue(player.Value, out var client) || client.PlayerObject == null)
                return;

            var diver = client.PlayerObject.GetComponent<NetworkPlayer>();
            if (diver == null || !diver.IsSpawned) return;

            var loadout = economy.LoadoutStateFor(player);
            var definitions = new List<EquipmentDefinition>();
            foreach (var equipmentId in loadout.EquippedIds)
                if (economy.TryGetEquipmentDefinition(equipmentId, out var definition))
                    definitions.Add(definition);

            diver.ApplyLoadoutServer(loadout, definitions.ToArray());
        }

        private void Unsubscribe()
        {
            if (economy != null && subscribed) economy.OnLoadoutChanged -= LoadoutChanged;
            subscribed = false;
        }

        private void OnDisable()
        {
            EconomyPurchaseAuthority.Unbind(HandlePurchase);
            Unsubscribe();
        }

        private void OnDestroy()
        {
            EconomyPurchaseAuthority.Unbind(HandlePurchase);
            Unsubscribe();
        }
    }
}

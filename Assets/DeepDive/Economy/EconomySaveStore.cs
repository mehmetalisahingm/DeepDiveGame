using System;
using System.IO;
using UnityEngine;

namespace DeepDive.Economy
{
    [Serializable]
    public sealed class EconomySaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string campaignId = "";
        public string checkpointId = "";
        public int sharedBalance;
        public int revision;
        public string[] hostEquipmentIds = Array.Empty<string>();
        public string[] soldCaptureIds = Array.Empty<string>();
        public string[] paidRecordingIds = Array.Empty<string>();
    }

    // P3 v1 local-host persistence. The campaign lives on the host machine (CONTRACTS D06).
    // Writes go to a temporary file first, are parsed/validated, then replace the primary while
    // preserving the last known-good file as .bak. Loading falls back to that backup.
    public static class EconomySaveStore
    {
        public static bool IsValid(EconomySaveData data) =>
            data != null &&
            data.schemaVersion == EconomySaveData.CurrentSchemaVersion &&
            !string.IsNullOrWhiteSpace(data.campaignId) &&
            !string.IsNullOrWhiteSpace(data.checkpointId) &&
            data.sharedBalance >= 0 && data.revision >= 0;

        public static bool TryWriteAtomic(string path, EconomySaveData data, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(path) || !IsValid(data))
            {
                error = "InvalidSaveData";
                return false;
            }

            var temp = path + ".tmp";
            var backup = path + ".bak";
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (!TryReadExact(temp, out var verified) || !IsValid(verified))
                {
                    error = "SaveVerificationFailed";
                    SafeDelete(temp);
                    return false;
                }

                if (File.Exists(path))
                {
                    try
                    {
                        // On supported desktop filesystems this is the safest replacement: the
                        // old primary becomes .bak in the same operation.
                        File.Replace(temp, path, backup);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(path, backup, true);
                        File.Copy(temp, path, true);
                        SafeDelete(temp);
                    }
                    catch (IOException)
                    {
                        File.Copy(path, backup, true);
                        File.Copy(temp, path, true);
                        SafeDelete(temp);
                    }
                }
                else File.Move(temp, path);

                if (!TryReadExact(path, out var finalData) || !IsValid(finalData))
                {
                    error = "FinalSaveVerificationFailed";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name + ": " + exception.Message;
                SafeDelete(temp);
                return false;
            }
        }

        public static bool TryLoad(string path, out EconomySaveData data, out bool usedBackup)
        {
            usedBackup = false;
            if (TryReadExact(path, out data) && IsValid(data)) return true;

            if (TryReadExact(path + ".bak", out data) && IsValid(data))
            {
                usedBackup = true;
                return true;
            }

            data = null;
            return false;
        }

        private static bool TryReadExact(string path, out EconomySaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            try
            {
                data = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(path));
                return data != null;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}

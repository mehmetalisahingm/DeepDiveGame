using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Composition
{
    // #278 (Mert): what the acting player is told after a real, host-accepted turn-in. Pure rules, no authority: the numbers come from the
    // replicated outcome the economy already decided (amount, item count) and the replicated balance, nothing is granted or counted here.
    public readonly struct SuccessCard
    {
        public readonly string Title, Detail, NextGoal;
        public readonly int Before, After, Delta;
        public readonly bool First;

        public SuccessCard(string title, string detail, int before, int after, bool first, string nextGoal)
        {
            Title = title; Detail = detail; Before = before; After = after; Delta = after - before; First = first; NextGoal = nextGoal;
        }
    }

    public static class SuccessFeedbackRules
    {
        // The camera ladder's prices (the same ids and prices as the EconomyManager catalog and the store shelves).
        public const int CameraBasicPrice = 150, CameraAdvancedPrice = 400, CameraProPrice = 900;

        // Only an accepted fish or recording turn-in with a real credit is a success. A refusal, a zero credit or any other service is not.
        public static bool TryBuild(ServicePointType type, bool accepted, int amount, int items, int balanceAfter, int cameraTier, bool firstSoFar, out SuccessCard card)
        {
            card = default;
            if (!accepted || amount <= 0 || items <= 0) return false;
            string title, detail;
            switch (type)
            {
                case ServicePointType.FishBuyer: title = "AV SATILDI!"; detail = items == 1 ? "1 av teslim edildi" : items + " av teslim edildi"; break;
                case ServicePointType.RecordingBuyer: title = "KAYIT TESLIM!"; detail = items == 1 ? "1 kayit teslim edildi" : items + " kayit teslim edildi"; break;
                default: return false;
            }
            if (firstSoFar) title = "ILK BASARI! " + title;
            card = new SuccessCard(title, detail, balanceAfter - amount, balanceAfter, firstSoFar, NextGoal(balanceAfter, cameraTier));
            return true;
        }

        // The next concrete thing the money is for: the next camera of the ladder, with what is still missing or "go and buy it".
        public static string NextGoal(int balance, int cameraTier)
        {
            string name; int price; string use;
            switch (cameraTier)
            {
                case 0: name = "Temel kamera"; price = CameraBasicPrice; use = "av ve klip cekmeni saglar"; break;
                case 1: name = "Gelismis kamera"; price = CameraAdvancedPrice; use = "daha net ve daha cok izlenen klipler"; break;
                case 2: name = "Profesyonel kamera"; price = CameraProPrice; use = "en yuksek klip kalitesi"; break;
                default: return "SIRADAKI HEDEF: sandali onar, yeni tekne ve kanal hedefi";
            }
            return balance >= price
                ? $"SIRADAKI HEDEF: {name} ({price}) - paran yetiyor, dukkana git: {use}"
                : $"SIRADAKI HEDEF: {name} ({price}) - {price - balance} kredi eksik: {use}";
        }

        // A short rising chime, generated (no asset to license or ship): C-E-G, and a higher C on top for the first success of the session.
        public static AudioClip CreateChime(bool first)
        {
            const int rate = 44100;
            var notes = first ? new[] { 523.25f, 659.25f, 783.99f, 1046.5f } : new[] { 659.25f, 880f };
            const float step = 0.11f, tail = 0.35f;
            var length = (int)((step * (notes.Length - 1) + tail) * rate);
            var data = new float[length];
            for (var n = 0; n < notes.Length; n++)
            {
                var start = (int)(n * step * rate);
                for (var i = start; i < length; i++)
                {
                    var t = (i - start) / (float)rate;
                    var envelope = Mathf.Exp(-t * 7f) * Mathf.Min(1f, t * 400f);
                    data[i] += (Mathf.Sin(2f * Mathf.PI * notes[n] * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * notes[n] * t)) * envelope * 0.22f;
                }
            }
            var clip = AudioClip.Create(first ? "SuccessChimeFirst" : "SuccessChime", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

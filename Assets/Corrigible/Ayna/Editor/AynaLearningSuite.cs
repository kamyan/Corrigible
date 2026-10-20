using System;
using System.Collections.Generic;
using Corrigible.Ayna.Core;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace Corrigible.Ayna.EditorTools
{
    public static class AynaLearningSuite
    {
        static Queue<LearningBenchOptions> queue;
        static readonly List<string> outputs = new List<string>();
        static bool waiting;
        static string error;
        public static string Start(int series = 100, bool tactical = true)
        {
            AynaSelfPlay.AssertEditorSafe(); if (queue != null) throw new InvalidOperationException("suite busy");
            if ((bool)JObject.Parse(AynaLearningBench.Status())["Running"]) throw new InvalidOperationException("benchmark busy");
            queue = new Queue<LearningBenchOptions>(); outputs.Clear(); error = null; waiting = false;
            foreach (var bot in new[] { "Habit", "Random", "Surprise", "Greedy" }) queue.Enqueue(new LearningBenchOptions { Series = series, Bot = bot, Tactical = tactical, Directory = "Docs/AYNA/denge/F3/final" });
            foreach (var bot in new[] { "Habit", "Random" }) queue.Enqueue(new LearningBenchOptions { Series = series, Bot = bot, Tactical = tactical, Learning = LearningLevel.Off, Directory = "Docs/AYNA/denge/F3/control" });
            EditorApplication.update += Tick; return Status();
        }
        public static string Status() => AynaJson.Write(new { Running = queue != null, Remaining = queue?.Count ?? 0, Outputs = outputs.ToArray(), Error = error, Active = JObject.Parse(AynaLearningBench.Status()) });
        public static void Cancel() { AynaLearningBench.Cancel(); EditorApplication.update -= Tick; queue = null; }
        static void Tick()
        {
            try
            {
                AynaSelfPlay.AssertEditorSafe(); var status = JObject.Parse(AynaLearningBench.Status());
                if ((bool)status["Running"]) return;
                if (waiting)
                {
                    if (status["Error"].Type != JTokenType.Null) throw new InvalidOperationException((string)status["Error"]);
                    outputs.Add((string)status["Output"]); waiting = false;
                }
                if (queue.Count == 0) { EditorApplication.update -= Tick; queue = null; return; }
                AynaLearningBench.Start(queue.Dequeue()); waiting = true;
            }
            catch (Exception ex) { error = ex.ToString(); Cancel(); }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>F3 örnek sahnesindeki niyet günlüğü. Gerçek oyundaki komut kaydının yerine geçmez.</summary>
    public sealed class MockSadeDeskCommands : MonoBehaviour
    {
        public readonly struct Entry
        {
            public readonly string Command, Argument;
            public Entry(string command, string argument) { Command = command; Argument = argument; }
        }
        readonly List<Entry> entries = new List<Entry>();
        public IReadOnlyList<Entry> Entries => entries;
        public event Action<string, string> Executed;
        public void Run(string command, string argument = null)
        {
            entries.Add(new Entry(command, argument));
            Executed?.Invoke(command, argument);
        }
    }
}

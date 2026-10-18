using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace WCFTablicServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class TablicService : ITablicService
    {
        private static readonly object LockObj = new object();
        private static readonly Dictionary<string, ITablicCallback> Players = new Dictionary<string, ITablicCallback>();
        private static readonly Dictionary<string, ITablicCallback> Observers = new Dictionary<string, ITablicCallback>();
        private static string waitingPlayer;
        private static Game activeGame;

        private string name;
        private bool observer;

        public OperationResult PrijaviSeZaIgru(string ime)
        {
            if (string.IsNullOrWhiteSpace(ime))
            {
                return Error("Ime je obavezno.");
            }

            name = ime.Trim();
            observer = false;
            ITablicCallback callback = OperationContext.Current.GetCallbackChannel<ITablicCallback>();

            string message;
            lock (LockObj)
            {
                Players[name] = callback;

                if (activeGame != null)
                {
                    return Error("Partija je vec aktivna. Mozes se prijaviti kao posmatrac.");
                }

                if (waitingPlayer == null)
                {
                    waitingPlayer = name;
                    message = "Cekas drugog igraca.";
                }
                else if (waitingPlayer == name)
                {
                    message = "Vec cekas drugog igraca.";
                }
                else
                {
                    StartGame(waitingPlayer, name);
                    waitingPlayer = null;
                    message = "Partija je pocela.";
                }
            }

            NotifyAll();
            return Ok(message);
        }

        public OperationResult PrijaviSeKaoPosmatrac(string ime)
        {
            if (string.IsNullOrWhiteSpace(ime))
            {
                return Error("Ime je obavezno.");
            }

            name = ime.Trim();
            observer = true;
            ITablicCallback callback = OperationContext.Current.GetCallbackChannel<ITablicCallback>();

            lock (LockObj)
            {
                Observers[name] = callback;
            }

            return Ok("Registrovan si kao posmatrac.");
        }

        public OperationResult BaciKartu(string karta)
        {
            string result;
            lock (LockObj)
            {
                OperationResult validation = ValidateTurn(karta);
                if (validation.IsError)
                {
                    return validation;
                }

                activeGame.Hands[name].Remove(karta);
                activeGame.Table.Add(karta);
                activeGame.LastMessage = name + " je bacio kartu " + karta + ".";
                SwitchTurn();
                result = activeGame.LastMessage;
            }

            NotifyAll();
            return Ok(result);
        }

        public OperationResult Nosi(string kartaIzRuke, List<string> karteSaTalona)
        {
            string result;
            lock (LockObj)
            {
                OperationResult validation = ValidateTurn(kartaIzRuke);
                if (validation.IsError)
                {
                    return validation;
                }

                if (karteSaTalona == null || karteSaTalona.Count == 0)
                {
                    return Error("Moras izabrati bar jednu kartu sa talona.");
                }

                foreach (string karta in karteSaTalona)
                {
                    if (!activeGame.Table.Contains(karta))
                    {
                        return Error("Karta nije na talonu: " + karta);
                    }
                }

                int target = CardValue(kartaIzRuke);
                int sum = karteSaTalona.Sum(CardValue);
                if (sum != target)
                {
                    return Error("Potez nije legalan. Zbir izabranih karata mora biti jednak vrednosti karte iz ruke.");
                }

                activeGame.Hands[name].Remove(kartaIzRuke);
                foreach (string karta in karteSaTalona)
                {
                    activeGame.Table.Remove(karta);
                }

                activeGame.Scores[name] += karteSaTalona.Count + 1;
                activeGame.LastMessage = name + " je nosio sa " + kartaIzRuke + ": " + string.Join(" ", karteSaTalona);
                SwitchTurn();
                result = activeGame.LastMessage;
            }

            NotifyAll();
            return Ok(result);
        }

        public string VratiStanje()
        {
            lock (LockObj)
            {
                return StateFor(name, observer);
            }
        }

        private static void StartGame(string p1, string p2)
        {
            List<string> deck = CreateDeck();
            activeGame = new Game
            {
                Player1 = p1,
                Player2 = p2,
                Turn = p1,
                Table = deck.Take(4).ToList(),
                Hands = new Dictionary<string, List<string>>(),
                Scores = new Dictionary<string, int>(),
                LastMessage = "Partija je pocela."
            };

            activeGame.Hands[p1] = deck.Skip(4).Take(6).ToList();
            activeGame.Hands[p2] = deck.Skip(10).Take(6).ToList();
            activeGame.Scores[p1] = 0;
            activeGame.Scores[p2] = 0;
        }

        private OperationResult ValidateTurn(string karta)
        {
            if (activeGame == null)
            {
                return Error("Nema aktivne partije.");
            }

            if (observer || !activeGame.Hands.ContainsKey(name))
            {
                return Error("Samo igrac moze da odigra potez.");
            }

            if (activeGame.Turn != name)
            {
                return Error("Nisi na potezu.");
            }

            if (!activeGame.Hands[name].Contains(karta))
            {
                return Error("Nemate kartu u ruci: " + karta);
            }

            return Ok("Validno.");
        }

        private static void SwitchTurn()
        {
            if (activeGame.Hands[activeGame.Player1].Count == 0 && activeGame.Hands[activeGame.Player2].Count == 0)
            {
                activeGame.LastMessage += " Partija je zavrsena. Rezultat: " +
                    activeGame.Player1 + "=" + activeGame.Scores[activeGame.Player1] + ", " +
                    activeGame.Player2 + "=" + activeGame.Scores[activeGame.Player2] + ".";
                return;
            }

            activeGame.Turn = activeGame.Turn == activeGame.Player1 ? activeGame.Player2 : activeGame.Player1;
        }

        private static void NotifyAll()
        {
            List<Tuple<string, ITablicCallback, bool>> targets;
            lock (LockObj)
            {
                targets = Players.Select(p => Tuple.Create(p.Key, p.Value, false))
                    .Concat(Observers.Select(o => Tuple.Create(o.Key, o.Value, true)))
                    .ToList();
            }

            foreach (var target in targets)
            {
                try
                {
                    target.Item2.Obavestenje(StateFor(target.Item1, target.Item3));
                }
                catch (CommunicationException)
                {
                }
                catch (TimeoutException)
                {
                }
            }
        }

        private static string StateFor(string user, bool isObserver)
        {
            if (activeGame == null)
            {
                return waitingPlayer == null ? "Nema aktivne partije." : "Ceka se drugi igrac. Prvi igrac: " + waitingPlayer;
            }

            var sb = new StringBuilder();
            sb.AppendLine(activeGame.LastMessage);
            sb.AppendLine("Talon: " + string.Join(" ", activeGame.Table));
            sb.AppendLine("Na potezu: " + activeGame.Turn);
            sb.AppendLine("Igraci: " + activeGame.Player1 + " vs " + activeGame.Player2);
            sb.AppendLine("Rezultat: " + activeGame.Player1 + "=" + activeGame.Scores[activeGame.Player1] + ", " + activeGame.Player2 + "=" + activeGame.Scores[activeGame.Player2]);

            if (!isObserver && activeGame.Hands.ContainsKey(user))
            {
                sb.AppendLine("Tvoje karte: " + string.Join(" ", activeGame.Hands[user]));
            }
            else
            {
                sb.AppendLine("Posmatras partiju.");
            }

            return sb.ToString();
        }

        private static int CardValue(string card)
        {
            string digits = new string(card.TakeWhile(char.IsDigit).ToArray());
            int value;
            return int.TryParse(digits, out value) ? value : 0;
        }

        private static List<string> CreateDeck()
        {
            var cards = new List<string>();
            string[] suits = { "P", "K", "H", "T" };
            foreach (string suit in suits)
            {
                for (int value = 1; value <= 14; value++)
                {
                    cards.Add(value + suit);
                }
            }

            return cards.OrderBy(c => Guid.NewGuid()).ToList();
        }

        private static OperationResult Ok(string message)
        {
            return new OperationResult { IsError = false, Message = message, State = StateFor(null, true) };
        }

        private static OperationResult Error(string message)
        {
            return new OperationResult { IsError = true, Message = message, State = StateFor(null, true) };
        }

        private class Game
        {
            public string Player1 { get; set; }
            public string Player2 { get; set; }
            public string Turn { get; set; }
            public List<string> Table { get; set; }
            public Dictionary<string, List<string>> Hands { get; set; }
            public Dictionary<string, int> Scores { get; set; }
            public string LastMessage { get; set; }
        }
    }
}

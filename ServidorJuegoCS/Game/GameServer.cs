using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Threading;

namespace ServidorJuegoCS.Game
{

    public class GameServer : Raiser
    {

        bool playing = false;

        public Tablero Hidden { get; set; } = new();
        public Tablero Visible { get; set; } = null;

        public ObservableCollection<Player> Players { get; private set; } = new();

        public bool Playing { get => playing; private set { playing = value; RaiseProperty(); } }
        public TimeSpan PlayTime { get; set; } = TimeSpan.FromSeconds(60);
        public ulong PlayScore { get; set; } = 100;

        Stopwatch counter = new();
        HttpListener listener = null;
        System.Timers.Timer timer = new();
        System.Timers.Timer counterback = new();
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int seconds;
        public int Seconds { get => seconds; set { seconds = value; RaiseProperty(); } }

        public ICommand IniciarServidorCommnad { get; }
        public ICommand IniciarPartidaCommnad { get; }
        public ICommand DetenerPartidaCommnad { get; }
        public ICommand DetenerServidorCommnad { get; }

        public GameServer()
        {
            IniciarServidorCommnad = new RelayCommand(StartListen);
            IniciarPartidaCommnad = new RelayCommand(Start);
            DetenerPartidaCommnad = new RelayCommand(Stop);
            DetenerServidorCommnad = new RelayCommand(StopListen);
            timer.Elapsed += (s, e) =>
            {
                Stop();
            };
            counterback.Interval = 1000;
            counterback.Elapsed += (s, e) =>
            {
                seconds--;
            };
        }

        public ulong GetScoreByElpasedTime(TimeSpan elapsed)
        {
            if (elapsed > PlayTime) return 0;
            return (ulong)((PlayTime - elapsed) * PlayScore / PlayTime);
        }

        /// <summary>
        /// Agrega un nuevo jugador a la partida actual.
        /// </summary>
        /// <param name="playername">El nombre del jugador que se quiere agregar</param>
        /// <returns>El nombre del jugadador modificado si es necesario</returns>
        public string AddPlayerByName(string playername)
        {
            Player player = new()
            {
                Score = 0
            };
            var count = Players.Count(x => x.Name == playername);
            if (count > 0)
            {
                player.Name = $"{playername} {count + 1}";
            }
            else
            {
                player.Name = playername;
            }
            Players.Add(player);
            return player.Name;
        }

        public void Start()
        {
            if (Playing == false)
            {
                //if (Hidden.Validate() == false)
                //{
                //    throw new InvalidProgramException("Las operaciones del crucigrama deben ser validas");
                //}
                Playing = true;
                Visible = Hidden.HideRandom();
                foreach (var p in Players)
                {
                    p.Guessed = false;
                }
                timer.Interval = PlayTime.TotalMilliseconds;
                timer.Start();
                counterback.Start();
                counter.Restart();
            }
        }

        public void StartListen()
        {
            if (listener == null)
            {
                listener = new();
                listener.Prefixes.Add("http://localhost:8000/");
                listener.Start();
                Task.Run(() =>
                {
                    try
                    {
                        while (listener.IsListening)
                        {
                            var context = listener.GetContext();
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            if (context.Request.Url.LocalPath == "/Join")
                            {
                                var playername = context.Request.QueryString["Name"];
                                if (playername != null)
                                {
                                    string name = "";
                                    dispatcher.Invoke(() =>
                                    {
                                        name = AddPlayerByName(playername);
                                    });
                                    var json = JsonSerializer.Serialize(name);
                                    var data = Encoding.UTF8.GetBytes(json);
                                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                                    context.Response.OutputStream.Write(data);
                                    context.Response.ContentType = "application/json";
                                }
                            }
                            else if (context.Request.Url.LocalPath == "/Guessing")
                            {
                                if (Playing && Visible != null)
                                {
                                    var json = JsonSerializer.Serialize(Visible.Numbers);
                                    var data = Encoding.UTF8.GetBytes(json);
                                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                                    context.Response.OutputStream.Write(data);
                                    context.Response.ContentType = "application/json";
                                }
                                else
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;

                                }
                            }
                            else if (context.Request.Url.LocalPath == "/Play" && context.Request.HttpMethod == "POST")
                            {
                                if (Playing)
                                {
                                    /**/
                                    var buffer = new byte[1024];
                                    int x = context.Request.InputStream.Read(buffer, 0, buffer.Length);
                                    var guessjson = Encoding.UTF8.GetString(buffer, 0, x);
                                    var guess = JsonSerializer.Deserialize<Jugada>(guessjson);
                                    /**/
                                    if (guess.Player != null && guess.Numbers != null)
                                    {
                                        bool? guessed = false;
                                        dispatcher.Invoke(() =>
                                        {
                                            guessed = PlayerGuess(guess);
                                        });
                                        if (guessed == true)
                                        {
                                            context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                                        }
                                        else if (guessed == false)
                                        {
                                            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                                        }
                                        else
                                        {
                                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                                        }
                                    }
                                }
                            }
                            else if (context.Request.Url.LocalPath == "/Leave" && context.Request.HttpMethod == "DELETE")
                            {
                                var playername = context.Request.QueryString["Name"];
                                if (playername != null)
                                {
                                    bool result = false;
                                    dispatcher.Invoke(() =>
                                    {
                                        result = RemovePlayerByName(playername);
                                    });
                                    if (result)
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.OK;

                                    }
                                    else
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                                    }
                                }
                            }
                            context.Response.Close();
                        }
                    }
                    catch { }
                });
            }
        }

        public void StopListen()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }
        }

        public void Stop()
        {
            if (Playing)
            {
                Playing = false;
                timer.Stop();
                counter.Stop();
                counterback.Stop();
            }
        }

        /// <summary>
        /// Establece la puntacion de todos los jugadores a 0 sin sacarlos de la partica actual.
        /// </summary>
        public void ResetScores()
        {
            foreach (var p in Players)
            {
                p.Score = 0;
            }
        }

        /// <summary>
        /// Saca de la partida actual a todos los jugadores.
        /// </summary>
        public void ResetPlayers()
        {
            Players.Clear();
        }

        /// <summary>
        /// Inteta adivinar la frase para un jugador en especifico por nombre.
        /// </summary>
        /// <param name="playername">El nombre del jugador que esta intentando adivinar</param>
        /// <param name="guess">La frase que con la que el jugador esta intentando adivinar</param>
        /// <returns>True si ha logrado adivinar. False si no logro adivinar. Null si el jugador no existe</returns>
        public bool? PlayerGuess(Jugada guess)
        {
            var elpased = counter.Elapsed;
            var player = Players.FirstOrDefault(x => x.Name == guess.Player);
            if (player == null) return false;
            if (!player.Guessed)
            {
                for (var i = 0; i < Hidden.Numbers.Length; i++)
                {
                    if (Hidden.Numbers[i] != guess.Numbers[i]) return false;
                }
                player.Guessed = true;
                player.Score += GetScoreByElpasedTime(elpased);
                return true;
            }
            return null;
        }

        public bool RemovePlayerByName(string name)
        {
            List<Player> toremove = new();
            foreach (var p in Players)
            {
                if (p.Name == name)
                {
                    toremove.Add(p);
                }
            }
            foreach (var p in toremove)
            {
                Players.Remove(p);
            }
            return toremove.Count > 0;
        }

    }

}
using GalaSoft.MvvmLight.Command;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClienteWPFJuegoCS.Game
{

    public class GameClient : Raiser
    {

        string name = "PIPO";
        string log;
        bool connected;
        bool guessed;

        public string IP { get; set; } = "localhost";
        public string Name { get => name; set { name = value; RaiseProperty(); } }

        public Tablero Visible { get; set; } = new();
        public Tablero Guess { get; set; } = new();

        public string Log { get => log; set { log = value; RaiseProperty(); } }
        public bool Connected { get => connected; set { connected = value; RaiseProperty(); } }
        public bool Guessed { get => guessed; set { guessed = value; RaiseProperty(); } }

        public ICommand ConectarCommand { get; set; }
        public ICommand PedirCommand { get; set; }
        public ICommand EnviarCommand { get; set; }
        public ICommand DesconectarCommand { get; set; }

        System.Timers.Timer timer = new();

        public GameClient()
        {
            ConectarCommand = new RelayCommand(Join);
            PedirCommand = new RelayCommand(Guessing);
            EnviarCommand = new RelayCommand(Play);
            DesconectarCommand = new RelayCommand(Leave);
            timer.Interval = 1000;
            timer.Elapsed += (s, e) =>
            {
                Guessing();
            };
        }


        public async void Join()
        {
            using HttpClient client = new();
            try
            {
                var respone = await client.GetAsync($"http://{IP}:8000/Join?Name={Name}");
                if (respone.StatusCode == HttpStatusCode.OK)
                {
                    /* Pudo unirse a la partida */
                    var json = await respone.Content.ReadAsStringAsync();
                    Name = JsonSerializer.Deserialize<string>(json);
                    Guessed = false;
                    Connected = true;
                    //timer.Start();
                    //return true;
                    Log = "Se ha unido a la partida";
                    Guessing();
                    return;
                }
                //return false;
                Log = "No se ha podido unir a la partida";
            }
            catch (Exception ex)
            {
                Log = "No se ha podido conectar con sel servidor. Revise su conexion a internte";
            }
        }

        public async void Guessing()
        {
            await GuessingAsync();
        }

        public async Task GuessingAsync()
        {
            //Play(); // Checar si cambio de ronda
            //if (!Guessed)
            //{
            using HttpClient client = new();
            try
            {
            Loop:
                var respone = await client.GetAsync($"http://{IP}:8000/Guessing");
                if (respone.StatusCode == HttpStatusCode.OK)
                {
                    /* La partida ya ha empezado */
                    //timer.Stop();
                    var json = await respone.Content.ReadAsStringAsync();
                    Visible.Numbers = JsonSerializer.Deserialize<int?[]>(json);
                    Array.Copy(Visible.Numbers, Guess.Numbers, 9);
                    Guess.RaiseProperty(null);
                    //return true;
                    Log = "Intente llenar el crucigrama";
                    return;
                }
                else if (respone.StatusCode == HttpStatusCode.Conflict)
                {
                    /* La partida aun no ha empezado */
                    //return false;
                    Log = "La ronda ya ha acabado. Espere a que empiece una nueva";
                    Task.Delay(100);
                    goto Loop;
                    return;
                }
                /* Error en el servidor */
                //return null;
                Log = "Error inesperado. Intente enviar de nuevo el crucigrama";
            }
            catch
            {
                Log = "No se ha podido conectar con sel servidor. Revise su conexion a internte";
            }
            //}
        }

        public async void Play()
        {
            using HttpClient client = new();
            try
            {
                /**/
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{IP}:8000/Play"),
                    Content = new StringContent(JsonSerializer.Serialize(new Jugada { Player = Name, Numbers = Guess.Numbers }), Encoding.UTF8, "application/json")
                };
                /**/
                var respone = await client.SendAsync(request);
                if (respone.StatusCode == HttpStatusCode.Accepted)
                {
                    /* Palabra correcta */
                    //return true;
                    Guessed = true;
                    //if(!timer.Enabled) timer.Start();
                    await GuessingAsync();
                    Log = "Felicidades su respuesta es correcta";
                    return;
                }
                else if (respone.StatusCode == HttpStatusCode.OK)
                {
                    /* Palabra equivocada */
                    //return false;
                    Guessed = true;
                    //if(!timer.Enabled) timer.Start();
                    await GuessingAsync();
                    Log = "Su respuesta ya ha sido aceptada";
                    return;
                }
                else if (respone.StatusCode == HttpStatusCode.Conflict)
                {
                    /* Palabra equivocada */
                    //return false;
                    Guessed = false;
                    //if(timer.Enabled) timer.Stop();
                    await GuessingAsync();
                    Log = "Su respuesta ha sido rechazada";
                    return;
                }
                /* El jugador ya no pertence a esta partida o error en el servidor */
                //return null;
                Log = "El servidor ha rechazado la respuesta. Intente enviar de nuevo el crucigrama o volver a unirse a la partida";
            }
            catch
            {
                Log = "No se ha podido conectar con sel servidor. Revise su conexion a internte";
            }
        }

        public async void Leave()
        {
            using HttpClient client = new();
            try
            {
                var respone = await client.DeleteAsync($"http://{IP}:8000/Leave?Name={Name}");
                if (respone.StatusCode == HttpStatusCode.OK)
                {
                    /* El jugador ha salido de la partida */
                    //return true;
                    Connected = false;
                    Log = "Ha abandonado la partida";
                    return;
                }
                else if (respone.StatusCode == HttpStatusCode.Conflict)
                {
                    Connected = false;
                    /* El jugador ya no pertence a esta partida */
                    Log = "Ya no pertence a esta partida";
                    return;
                }
                //return false;
                Log = "Error inesperado en el servidor. Intente de nuevo";
                return;
            }
            catch
            {
                Log = "No se ha podido conectar con sel servidor. Revise su conexion a internte";
            }
        }

    }

}
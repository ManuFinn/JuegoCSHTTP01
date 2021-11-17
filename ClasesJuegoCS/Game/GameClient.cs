using System;
using System.Net;
using System.Text.Json;

namespace ClasesJuegoCS.Game
{

    public class GameClient
    {

        public string IP { get; set; }
        public ulong Port { get; set; }
        public string Name { get; set; }
        public string Guess { get; set; }

        public List<string> ImageUrls { get; set; } = new();

        public async Task<bool> Join()
        {
            using HttpClient client = new();
            var respone = await client.GetAsync($"http://{IP}:{Port}/Join?Name={Name}");
            if (respone.StatusCode == HttpStatusCode.OK)
            {
                /* Pudo unirse a la partida */
                var json = await respone.Content.ReadAsStringAsync();
                Name = JsonSerializer.Deserialize<string>(json);
                return true;
            }
            return false;
        }

        public async Task<bool?> Guessing()
        {
            using HttpClient client = new();
            var respone = await client.GetAsync($"http://{IP}:{Port}/Guessing");
            if (respone.StatusCode == HttpStatusCode.OK)
            {
                /* La partida ya ha empezado */
                var json = await respone.Content.ReadAsStringAsync();
                ImageUrls = JsonSerializer.Deserialize<List<string>>(json);
                return true;
            }
            else if (respone.StatusCode == HttpStatusCode.Conflict)
            {
                /* La partida aun no ha empezado */
                return false;
            }
            return null;
        }

        public async Task<bool?> Play()
        {
            using HttpClient client = new();
            var respone = await client.GetAsync($"http://{IP}:{Port}/Play?Name={Name}&Guess={Guess}");
            if (respone.StatusCode == HttpStatusCode.OK)
            {
                /* Palabra correcta */
                return true;
            }
            else if (respone.StatusCode == HttpStatusCode.Conflict)
            {
                /* Palabra equivocada */
                return false;
            }
            /* El jugador ya no pertence a esta partida */
            return null;
        }

        public async Task<bool> Leave()
        {
            using HttpClient client = new();
            var respone = await client.GetAsync($"http://{IP}:{Port}/Leave?Name={Name}");
            if (respone.StatusCode == HttpStatusCode.OK)
            {
                /* El jugador ha salido de la partida */
                return true;
            }
            else if (respone.StatusCode == HttpStatusCode.Conflict)
            {
                /* El jugador ya no pertence a esta partida */
            }
            return false;
        }

    }

}
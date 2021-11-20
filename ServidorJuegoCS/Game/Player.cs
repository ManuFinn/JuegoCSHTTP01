using System;

namespace ServidorJuegoCS.Game
{

    public class Player : Raiser
    {

        ulong score;

        public bool Guessed {get; set;}
        public string Name { get; set; }
        public ulong Score { get => score; set { score = value; RaiseProperty(); } }

    }

}
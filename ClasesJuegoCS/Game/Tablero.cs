using System;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;

namespace ClasesJuegoCS.Game
{

    public class Tablero : Raiser
    {

        public int?[] Numbers { get; set; } = new int?[9];

        [JsonIgnore] public int? Number1 { get => Numbers[0]; set { Numbers[0] = value; RaiseProperty(); FillRow(0); FillColumn(0); } }
        [JsonIgnore] public int? Number2 { get => Numbers[1]; set { Numbers[1] = value; RaiseProperty(); FillRow(0); FillColumn(1); } }
        [JsonIgnore] public int? Number3 { get => Numbers[2]; set { Numbers[2] = value; RaiseProperty(); FillRow(0); FillColumn(2); } }
        [JsonIgnore] public int? Number4 { get => Numbers[3]; set { Numbers[3] = value; RaiseProperty(); FillRow(1); FillColumn(0); } }
        [JsonIgnore] public int? Number5 { get => Numbers[4]; set { Numbers[4] = value; RaiseProperty(); FillRow(1); FillColumn(1); } }
        [JsonIgnore] public int? Number6 { get => Numbers[5]; set { Numbers[5] = value; RaiseProperty(); FillRow(1); FillColumn(2); } }
        [JsonIgnore] public int? Number7 { get => Numbers[6]; set { Numbers[6] = value; RaiseProperty(); FillRow(2); FillColumn(0); } }
        [JsonIgnore] public int? Number8 { get => Numbers[7]; set { Numbers[7] = value; RaiseProperty(); FillRow(2); FillColumn(1); } }
        [JsonIgnore] public int? Number9 { get => Numbers[8]; set { Numbers[8] = value; RaiseProperty(); FillRow(2); FillColumn(2); } }

        public void Clear()
        {
            Numbers = new int?[9];
        }

        public void FillRow(int row)
        {
            ref var A = ref Numbers[row * 3 + 0];
            ref var B = ref Numbers[row * 3 + 1];
            ref var C = ref Numbers[row * 3 + 2];
            //
            if (A == null)
            {
                if (B != null && C != null)
                {
                    A = C - B;
                    RaiseProperty();
                }
            }
            else if (B == null)
            {
                if (A != null && C != null)
                {
                    B = C - A;
                    RaiseProperty();
                }
            }
            else if (C == null)
            {
                if (A != null && B != null)
                {
                    C = A + B;
                    RaiseProperty();
                }
            }
        }

        public void FillColumn(int column)
        {
            ref var A = ref Numbers[0 * 3 + column];
            ref var B = ref Numbers[1 * 3 + column];
            ref var C = ref Numbers[2 * 3 + column];
            //
            if (A == null)
            {
                if (B != null && C != null)
                {
                    A = C + B;
                    RaiseProperty();
                }
            }
            else if (B == null)
            {
                if (A != null && C != null)
                {
                    B = A - C;
                    RaiseProperty();
                }
            }
            else if (C == null)
            {
                if (A != null && B != null)
                {
                    C = A - B;
                    RaiseProperty();
                }
            }
        }

        public bool Validate()
        {
            if (Number1 + Number2 != Number3) return false;
            if (Number4 + Number5 != Number6) return false;
            if (Number7 + Number8 != Number9) return false;
            //
            if (Number1 - Number4 != Number7) return false;
            if (Number2 - Number5 != Number8) return false;
            if (Number3 - Number6 != Number9) return false;
            //
            return true;
        }

        public Tablero HideRandom() {

            Random random = new();
            List<int> selections = new();

            void Select(int min, int max) {
                var select = random.Next(min, max);
                while(selections.Any(x => x == select)) {
                    select = random.Next(min, max);
                }
                selections.Add(select);
            }

            Select(0, 3);
            Select(0, 3);
            Select(3, 6);
            Select(6, 9);
            Select(6, 9);

            Tablero tablero = new();

            foreach(var s in selections) {
                tablero.Numbers[s] = Numbers[s];
            }

            return tablero;

        }

    }

}
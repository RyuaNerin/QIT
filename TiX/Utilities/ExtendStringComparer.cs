using System;
using System.Collections.Generic;

namespace TiX.Utilities
{
    public sealed class ExtendStringComparer : IComparer<string>
    {
        public static readonly Comparison<string> Comparison = new Comparison<string>(CompareTo);
        public static readonly ExtendStringComparer Instance = new ExtendStringComparer();

        public int Compare(string x, string y)
        {
            return CompareTo(x, y);
        }
        
        private static int Cut(string x, int xindex)
        {
            bool isInt = char.IsDigit(x[xindex]);

            int nindex = xindex + 1;
            while (nindex < x.Length)
            {
                if (isInt != char.IsDigit(x[nindex]))
                    break;
                nindex++;
            }

            return Math.Min(nindex, x.Length) - xindex;
        }
        public static int CompareTo(string x, string y)
        {
            int xindex, yindex;
            int xlen, ylen;
            int xint, yint;

            int i;
            int k;
            int c;

            xindex = yindex = 0;
            while (xindex < x.Length && yindex < y.Length)
            {
                xlen = Cut(x, xindex);
                ylen = Cut(y, yindex);

                if (xlen == 0 || ylen == 0)
                    c = xlen.CompareTo(ylen);

                else if (char.IsDigit(x[xindex]) && char.IsDigit(y[yindex]) && int.TryParse(x.Substring(xindex, xlen), out xint) && int.TryParse(y.Substring(yindex, ylen), out yint))
                    c = xint.CompareTo(yint);

                else
                {
                    k = Math.Min(xlen, ylen);
                    c = 0;
                    for (i = 0; i < k; ++i)
                    {
                        if (x[xindex + i] != y[yindex + i])
                        {
                            c = x[xindex + i].CompareTo(y[yindex + i]);
                            break;
                        }
                    }

                    if (c == 0)
                        c = xlen.CompareTo(ylen);

                }

                xindex += xlen;
                yindex += ylen;

                if (c != 0)
                    return c;

            }

            return x.Length - y.Length;
        }
    }
}

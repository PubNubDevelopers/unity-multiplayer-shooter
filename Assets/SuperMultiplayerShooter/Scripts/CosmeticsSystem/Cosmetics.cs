using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Cosmetics
    /// - Used by the PlayerInstance class.
    /// - Downloaded and readable version of a player's cosmetic data.
    /// </summary>

    public class Cosmetics
    {
        public int hat;
        public int backpack;        // sample only, no effect at the moment
                                    // add more items here...

        public Cosmetics(int hat = -1, int backpack = -1)
        {
            this.hat = hat;
            this.backpack = backpack;
        }
    }
}
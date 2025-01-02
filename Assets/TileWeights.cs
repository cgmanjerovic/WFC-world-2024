using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TileWeights {
    public Dictionary<string, int> dict;

    public TileWeights()
    {
        dict = new Dictionary<string, int>() {
            {"ground",              10},
            {"b_corner",            4},
            {"b_arch_x",            4},
            {"b_arch_y",            4},
            {"b_path_x",            6},
            {"b_path_y",            6},
            {"b_stairs_xneg",       3},
            {"b_stairs_xpos",       3},
            {"b_stairs_yneg",       3},
            {"b_stairs_ypos",       3},
            {"flowers",             1},
            {"mushroomLarge",       1},
            {"mushrooms",           1},
            {"mushroomSingle",      1},
            {"path1",               1},
            {"path2",               1},
            {"path3",               1},
            {"rockAndFlowers",      1},
            {"rockLarge",           1},
            {"rocksSmall",          1},
            {"treeLarge",           1},
            {"treeLarge2",          1},
            {"treeMedium",          1},
            {"treeSmallWFlowers",   1},
            {"treesSmall",          1}
        };
    }
}
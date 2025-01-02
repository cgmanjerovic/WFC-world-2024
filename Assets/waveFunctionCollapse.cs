using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using Debug = UnityEngine.Debug;

//Original script by Caroline and Cecil Manjerovic 5-5-24
//Edited version by Caroline Manjerovic 12-8-24

//Main driver of the wave function collapse algorithm. Calls helper functions from Tile, changesMade, and TileRuleList
//Also the main driver of cliff detection after WFC is done. calls helper functions from raiseElevation.

//attached to an empty game object in the scene.

public class waveFunctionCollapse : MonoBehaviour
{
    public const int NUM_TILES = 39; //number of different tiles we can place
    public const float TILE_SIZE = 2f; //the size of the tiles in the scene
    public const float LEVEL_DIFF = 2.4f; //the height of the cliffs in the scene
    public const int MAP_SIZE = 30; //the size of the map in number of tiles
    public int REVERT_THRESH = 1; //number of times a step in the algorithm can be retried from before it is reverted
    public int REVERT_THRESH_OVERALL = 300; //the number of times the entire algorithm can restart before it is aborted and starts over
    public int NUM_TIMES_UNDO = 20; //the number of steps that are undone on a contradiction
    
    public GameObject[] tiles = new GameObject[NUM_TILES]; //array of gameobjects

    Tile[,] mapTileInfo = new Tile[MAP_SIZE, MAP_SIZE]; //nxn matrix of tile objects, represents the map
    GameObject[,] mapPrefabs = new GameObject[MAP_SIZE, MAP_SIZE]; //empty nxn matrix of the actual prefabs spawned in the game, represents the map

    bool isNetEntropyZero = false; //tells if the algorithm is finished or not
    bool abort = false; //whether the was a contradiction on a given step that needs to be handled
    bool tilesAllRaised = false; //whether the tiles have all be raised to the correct level during cliff detection

    private TileRuleList ruleSet = new TileRuleList(NUM_TILES); //class object containing the list of rules and tile weights

    Stack<changesMade> changeHistory = new Stack<changesMade>(); //contains the state of the map at each step using class objects
    int numTimesUndone = 0; //the number of undo's we have done this try

    // Start is called before the first frame update
    void Start()
    {
        //populate the map with empty class objects
        for (int i = 0; i < MAP_SIZE; i++)
        {
            for (int j = 0; j < MAP_SIZE; j++)
            {
                mapTileInfo[i, j] = new Tile(NUM_TILES, i, j);
            }
        }

        //need a null item in changeHistory to start
        changeHistory.Push(new changesMade(mapTileInfo, MAP_SIZE, null));
    }

    // Update is called once per frame----------------------------------------------------------------------------------------------------------
    void Update()
    {
        if (!isNetEntropyZero) //if not every tile is collapsed, the algorithm is not done
        {
            //GET SPACE WITH LOWEST ENTROPY----------------------------------------------------------------------------------------------------
            //Iterate through, get lowest value
            int lowestValue = NUM_TILES + 1;
            for (int i = 0; i < MAP_SIZE; i++)
            {
                for (int j = 0; j < MAP_SIZE; j++)
                {
                    //if the tile is lower than the current lowest                                            //without being collapsed
                    if (mapTileInfo[i, j].getEntropy() < lowestValue && mapTileInfo[i, j].getEntropy() > 0 && !mapTileInfo[i, j].isCollapsed())
                        lowestValue = mapTileInfo[i, j].getEntropy(); //that entropy is then the new lowest value
                }
            }

            //iterate again, getting list of all spaces of that value
            List<(int x, int y)> tilesWithLowestEntropy = new List<(int x, int y)>();
            for (int i = 0; i < MAP_SIZE; i++)
            {
                for (int j = 0; j < MAP_SIZE; j++)
                {
                    if (mapTileInfo[i, j].getEntropy() == lowestValue && !mapTileInfo[i, j].isCollapsed())
                    {
                        //contains a variable length list of coordinate pairs, corresponding to the tiles with the lowest entropy.
                        tilesWithLowestEntropy.Add((i, j));
                    }
                }
            }

            //COLLAPSE THE TILE--------------------------------------------------------------------------------------------------
            int whichTileToCollapse = UnityEngine.Random.Range(0, tilesWithLowestEntropy.Count); //choose which tile to collapse randomly from list
            int ttcx = tilesWithLowestEntropy[whichTileToCollapse].x; //get the tile to collapse's coordinates
            int ttcy = tilesWithLowestEntropy[whichTileToCollapse].y;
            Tile tileToCollapse = mapTileInfo[ttcx, ttcy]; //get the tile object corresponding to the tile to collapse

            //need to get the array index of the game object to instantiate from the tile object
            int instantiateThis;
            instantiateThis = tileToCollapse.collapse(ruleSet.getWeights(), changeHistory.Peek());

            //instantiate the tile gameObject, sotre it in mapPrefabs
            mapPrefabs[ttcx, ttcy] = Instantiate(tiles[instantiateThis],
                                                 new Vector3(TILE_SIZE * ttcx, 0, TILE_SIZE * ttcy),
                                                 Quaternion.Euler(new Vector3(-90, 0, 0)));

            //UPDATE MATRIX------------------------------------------------------------------------------------------------------------
            //update the entropy and possible tiles of each space on the board based on the tile just selected
            Stack<Tile> stack = new Stack<Tile>();
            stack.Push(tileToCollapse); //put the tile we just set on the stack

            while (stack.Count > 0)
            {
                Tile currentTile = stack.Pop();
                //get a list of directions, so we know which tiles to look at next
                List<(int x, int y)> directions = currentTile.getDirections(MAP_SIZE);
                //iterate through each of current tile's neighbors
                for (int i = 0; i < directions.Count; i++)
                {
                    //get the neighbor of that tile in given direction
                    int currentX = currentTile.getX() + directions[i].x;
                    int currentY = currentTile.getY() + directions[i].y;
                    //get its Tile object
                    Tile neighbor = mapTileInfo[currentX, currentY];

                    //use the constrain function to reduce its list of possible tiles and entropy.
                    //will return true if the function caused its entropy to go down. This means we need to check its neighbors as well, so it will be added to the stack
                    bool constrained = neighbor.constrain(currentTile.getPossibleTiles(), directions[i], ruleSet.getRuleSet(), ruleSet.getRuleSetBlackList());
                    //                                    its possible tiles              the direction  the ruleset           the rule set black list
                    //                                                                    its in 

                    if (constrained) //if the neighbor's entropy is reduced at all
                    {
                        stack.Push(neighbor); //push said neighbor onto the stack
                        //if the neighbor's entropy goes to 0 from this, we have a contradiction and need to restart
                        if (stack.Peek().getEntropy() == 0)
                        {
                            abort = true;
                            break; //no need to continue
                        }
                    }
                }
                if (abort) break;
            }

            //COMMIT THAT STEP----------------------------------------------------------------------------------------------------
            //store the state of the map at this step
            changeHistory.Push(new changesMade(mapTileInfo, MAP_SIZE, mapPrefabs[ttcx, ttcy]));
            //                                 map info               gameobject of tile just placed

            //UNDO IN CASE OF CONTRADICTION----------------------------------------------------------------------------------
            if (abort)
            {
                //Debug.Log("CONTRADICTION AT" + ttcx + ", " + ttcy);
                isNetEntropyZero = false; //make sure the algorithm doesn't stop
                abort = false; //mark the contradiction as dealt with

                changesMade stateToUndo; //will store the state to undo and delete
                changesMade stateToRevertTo = changeHistory.Peek(); //will sotre the state below that in the stack

                //undo NUM_TIMES_UNDO times
                for (int i = 0; i < NUM_TIMES_UNDO; i++)
                {
                    stateToUndo = changeHistory.Pop(); //get rid of the top state from the stack
                    stateToRevertTo = changeHistory.Peek(); //get the state below that

                    //destroy the object placed at the step represented by stateToUndo
                    GameObject.Destroy(stateToUndo.getGameObject());

                    //mark that we've had to undo once
                    numTimesUndone++;

                    //if we don't have anything else to pop, stop
                    if (changeHistory.Count <= 1) break;
                }

                while (stateToRevertTo.getNumTimesReverted() > REVERT_THRESH && changeHistory.Count > NUM_TIMES_UNDO) //keep going as long as the top state in the stack has already been reverted too many times, AND the stack isn't getting empty
                {
                    stateToUndo = changeHistory.Pop(); //get rid of the top state from the stack
                    GameObject.Destroy(stateToUndo.getGameObject());
                    numTimesUndone++;
                    stateToRevertTo = changeHistory.Peek(); //get the state below that
                }

                //set the map to the state we are at now
                mapTileInfo = stateToRevertTo.getMapTileInfo();
                //revert to said state (this function just markes stateToRevertTo)
                stateToRevertTo.revertToThisState();

                //restart case, if it's taken too long or we don't have enough left in the stack
                if (changeHistory.Count <= NUM_TIMES_UNDO || numTimesUndone > REVERT_THRESH_OVERALL)
                {
                    //reset numTimesUndone and clear the stack
                    numTimesUndone = 0;
                    changeHistory.Clear();
                    //need a null entry in the stack to start
                    changeHistory.Push(new changesMade(mapTileInfo, MAP_SIZE, null));
                    for (int i = 0; i < MAP_SIZE; i++)
                    {
                        for (int j = 0; j < MAP_SIZE; j++)
                        {
                            //delete all objects and reset the map info
                            mapTileInfo[i, j] = new Tile(NUM_TILES, i, j);
                            GameObject.Destroy(mapPrefabs[i, j]);
                        }
                    }
                }
            }

            //EVALUATE IF WE NEED TO CONTINUE-----------------------------------------------------------------------------------------------
            else
            {
                isNetEntropyZero = true;
                for (int i = 0; i < MAP_SIZE; i++)
                {
                    for (int j = 0; j < MAP_SIZE; j++)
                    {
                        //if any single tile is not collapsed, the algorithm is not complete
                        if (!mapTileInfo[i, j].isCollapsed())
                            isNetEntropyZero = false;
                    }
                }
            }
        } //WAVE FUNCTION COLLAPSE COMPLETE------------------------------------------------------------------------------------------------------------

        //RAISE THE LEVEL OF THE TILES TO MATCH CLIFFS---------------------------------------------------------------------------------------
        else if (!tilesAllRaised)//only do this is WFC is complete, and we haven't raised all the tiles already
        {
            //construct object containing functions
            raiseElevation re = new raiseElevation(mapTileInfo, ruleSet, MAP_SIZE);

            //scan the map
            for (int i = 0; i < MAP_SIZE; i++)
            {
                for (int j = 0; j < MAP_SIZE; j++)
                {
                                                    
                    if ((mapTileInfo[i, j].getChosenTile() <= 7) && //if tile is a cliff edge
                        !mapTileInfo[i, j].getHasBeenRaised()) //and hasn't been done already
                    {
                        //find the cliff line containing that tile
                        List<(int x, int y)> cliffLine = re.findCliffLine(i, j);
                        //raise the tiles within the cliffline a level
                        re.raiseTiles(cliffLine);
                    }
                }
            }

            //scan the map again to raise all tiles according to their level
            for (int i = 0;i < MAP_SIZE;i++)
            {
                for(int j = 0;j < MAP_SIZE;j++)
                {
                    mapPrefabs[i, j].GetComponent<Transform>().position = new Vector3 (mapPrefabs[i, j].GetComponent<Transform>().position.x,
                                                                                       mapTileInfo[i, j].getLevel() * LEVEL_DIFF, //raise it by a multiple of the level size
                                                                                       mapPrefabs[i, j].GetComponent<Transform>().position.z);
                }
            }
            tilesAllRaised = true; //mark the step as done
        }
    }
}

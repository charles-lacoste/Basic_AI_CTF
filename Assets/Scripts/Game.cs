using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class Game : MonoBehaviour {
    public static Game game;

    [SerializeField]
    public Text redScoreText, blueScoreText, winningText;

    public int scoreRed, scoreBlue;
    public float xBounds, yBounds;

    public bool redFlagTargeted, blueFlagTargeted, gameover, isBlueFlagTargeted, isRedFlagTargeted;

    public Flag redFlag, blueFlag;

    private List<NPC> redTeam, blueTeam;

    public GameObject arena, redFlagSpawn, blueFlagSpawn;

    public enum AIBehaviour {
        AI_BEHAVIOUR_1,
        AI_BEHAVIOUR_2
    };

    public AIBehaviour aiBehaviour;
    
    private void Start() {
        xBounds = arena.GetComponent<Renderer>().bounds.size.x / 2f;
        yBounds = arena.GetComponent<Renderer>().bounds.size.z / 2f;

        scoreRed = 0;
        scoreBlue = 0;

        blueTeam = new List<NPC>();
        redTeam = new List<NPC>();

        redFlagTargeted = blueFlagTargeted = isRedFlagTargeted = isBlueFlagTargeted = false;

        var npcs = FindObjectsOfType<NPC>();

        foreach (var npc in npcs)
            if (npc.gameObject.tag == "BlueTeam")
                blueTeam.Add(npc);
            else if (npc.gameObject.tag == "RedTeam")
                redTeam.Add(npc);

        aiBehaviour = AIBehaviour.AI_BEHAVIOUR_2;
    }

    private void Awake() {
        if (game != null && game != this) {
            Destroy(this.gameObject);
        } else {
            game = this;
        }
    }
    
    private void FixedUpdate() {
        if (gameover)
            return;

        var activeBlueFound = false;
        var activeRedFound = false;

        foreach (var blue in blueTeam)
            if (blue.state != NPC.State.FREEZE) {
                activeBlueFound = true;
                break;
            }

        foreach (var red in redTeam)
            if (red.state != NPC.State.FREEZE) {
                activeRedFound = true;
                break;
            }


        redScoreText.text = "Red: " + scoreRed;
        blueScoreText.text = "Blue: " + scoreBlue;

        if (!activeBlueFound) {
            winningText.text = "All Blue Team frozen. Red Team Wins!";
            gameover = true;
        }

        if (!activeRedFound) {
            winningText.text = "All Red Team frozen. Blue Team Wins!";
            gameover = true;
        }

        if (scoreRed > 4) {
            winningText.text = "Red Wins.";
            gameover = true;
        } else if (scoreBlue > 4) {
            winningText.text = "Blue Wins.";
            gameover = true;
        }


        if (Input.GetKeyDown(KeyCode.Alpha1))
            aiBehaviour = AIBehaviour.AI_BEHAVIOUR_1;
        if (Input.GetKeyDown(KeyCode.Alpha2))
            aiBehaviour = AIBehaviour.AI_BEHAVIOUR_2;

        isRedFlagTargeted = false;
        foreach (var blue in blueTeam) {
            if ((blue.target == null) && (blue.pursuer == null) && blue.isHome && !redFlagTargeted) {
                redFlagTargeted = true;
                blue.CaptureFlag();
                continue;
            }

            if ((blue.state != NPC.State.PURSUING) && (blue.state != NPC.State.CAPTURING) &&
                (blue.state != NPC.State.FREEZE)) {
                NPC closestSaveableNPC = null;

                foreach (var teammate in blueTeam) {
                    if ((blue == teammate) || (teammate.state != NPC.State.FREEZE))
                        continue;

                    if ((closestSaveableNPC == null) ||
                        ((blue.transform.position - teammate.transform.position).magnitude <
                         (blue.transform.position - closestSaveableNPC.transform.position).magnitude))
                        if (teammate.pursuer == null) {
                            teammate.pursuer = blue;
                            closestSaveableNPC = teammate;
                        }
                }

                if (closestSaveableNPC != null) {
                    blue.Unfreeze(closestSaveableNPC);
                    continue;
                }
            }

            if (blue.state == NPC.State.PURSUING)
                blue.StopPursuing();

            if ((blue.target == null) && (blue.pursuer == null) && blue.isHome &&
                (blue.state != NPC.State.CAPTURING) && (blue.state != NPC.State.FREEZE)) {
                NPC closesttargetableNPC = null;

                foreach (var red in redTeam)
                    if (!red.isHome && (red.state != NPC.State.FREEZE))
                        if ((closesttargetableNPC == null) ||
                            ((blue.transform.position - red.transform.position).magnitude <
                             (blue.transform.position - closesttargetableNPC.transform.position).magnitude))
                            if (red.pursuer == null)
                                closesttargetableNPC = red;

                if (closesttargetableNPC != null) {
                    blue.Pursue(closesttargetableNPC);
                    continue;
                }
            }
            if (blue.state == NPC.State.CAPTURING) {
                isRedFlagTargeted = true;
            }
        }

        isBlueFlagTargeted = false;
        foreach (var red in redTeam) {
            if ((red.target == null) && (red.pursuer == null) && red.isHome && !blueFlagTargeted) {
                blueFlagTargeted = true;
                red.CaptureFlag();
                continue;
            }

            if ((red.state != NPC.State.PURSUING) && (red.state != NPC.State.CAPTURING) &&
                (red.state != NPC.State.FREEZE)) {
                NPC closestSaveableNPC = null;

                foreach (var teammate in redTeam) {
                    if ((red == teammate) || (teammate.state != NPC.State.FREEZE))
                        continue;

                    if ((closestSaveableNPC == null) ||
                        ((red.transform.position - teammate.transform.position).magnitude <
                         (red.transform.position - closestSaveableNPC.transform.position).magnitude))
                        if (teammate.pursuer == null) {
                            teammate.pursuer = red;
                            closestSaveableNPC = teammate;
                        }
                }

                if (closestSaveableNPC != null) {
                    red.Unfreeze(closestSaveableNPC);
                    continue;
                }
            }

            if (red.state == NPC.State.PURSUING)
                red.StopPursuing();

            if ((red.target == null) && (red.pursuer == null) && red.isHome &&
                (red.state != NPC.State.CAPTURING) && (red.state != NPC.State.FREEZE)) {
                NPC closesttargetableNPC = null;

                foreach (var blue in blueTeam)
                    if (!blue.isHome && (blue.state != NPC.State.FREEZE))
                        if ((closesttargetableNPC == null) ||
                            ((red.transform.position - blue.transform.position).magnitude <
                             (red.transform.position - closesttargetableNPC.transform.position).magnitude))
                            if (blue.pursuer == null)
                                closesttargetableNPC = blue;


                if (closesttargetableNPC != null) {
                    red.Pursue(closesttargetableNPC);
                    continue;
                }
            }
            if (red.state == NPC.State.CAPTURING) {
                isBlueFlagTargeted = true;
            }
        }
        if (!isRedFlagTargeted)
            redFlagTargeted = false;
        if (!isBlueFlagTargeted)
            redFlagTargeted = false;
    }

    public void ScoreBlue() {
        scoreBlue++;
        redFlag.carrier.SetToWander();
        RestoreRedFlag();
    }

    public void ScoreRed() {
        scoreRed++;
        blueFlag.carrier.SetToWander();
        RestoreBlueFlag();
    }

    public void RestoreRedFlag() {
        redFlagTargeted = false;
        redFlag.carrier = null;
        redFlag.transform.position = redFlagSpawn.transform.position;
    }

    public void RestoreBlueFlag() {
        blueFlagTargeted = false;
        blueFlag.carrier = null;
        blueFlag.transform.position = blueFlagSpawn.transform.position;
    }

    private void SwitchBehaviour() {
        aiBehaviour = aiBehaviour == AIBehaviour.AI_BEHAVIOUR_1
            ? AIBehaviour.AI_BEHAVIOUR_2
            : AIBehaviour.AI_BEHAVIOUR_1;
    }
}
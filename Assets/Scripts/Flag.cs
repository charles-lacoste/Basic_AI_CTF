using UnityEngine;
using System.Collections;

public class Flag : MonoBehaviour {
    public NPC carrier;
    
    private void Start() {
    }
    
    private void Update() {
        if (carrier == null)
            return;

        //flag is above carrier's head
        transform.position = carrier.transform.position + new Vector3(0, 2, 0);

        if (carrier.state != NPC.State.CAPTURING)
            carrier.state = NPC.State.CAPTURING;

        if (!carrier.hasFlag)
            carrier.hasFlag = true;

        if ((carrier.gameObject.tag == "RedTeam") && (carrier.destination != Game.game.redFlagSpawn))
            carrier.destination = Game.game.redFlagSpawn;

        if ((carrier.gameObject.tag == "BlueTeam") && (carrier.destination != Game.game.blueFlagSpawn))
            carrier.destination = Game.game.blueFlagSpawn;
    }
}
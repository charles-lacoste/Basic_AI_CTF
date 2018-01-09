using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class NPC : MonoBehaviour {
    public enum State {
        WANDERING, //Moving randomly
        PURSUING, //Chasing an npc
        CAPTURING, //Grabing the flag
        FREEZE, //Can't move
        UNFREEZE //Trying to unfreeze an ally
    }

    public State state;
    public NPC target, pursuer;
    public GameObject destination;

    public Vector3 direction;
    private Vector3 desiredDirection, velocity;

    public bool hasFlag, isHome;

    public float speed, rotation;
    private float moveSpeed, maxAcceleration, nearSpeed, nearRadius, slowRadius;
    
    private void Start() {
        speed = 0;
        moveSpeed = 5;
        nearSpeed = 3;
        nearRadius = 3;
        maxAcceleration = 10;
        slowRadius = 2;

        hasFlag = false;
        state = State.WANDERING;

        desiredDirection = transform.forward;
        rotation = 0;
    }

    //Fixed update so that it updates at a consistent rate for calculations
    private void FixedUpdate() {
        if (Game.game.gameover)
            return;

        //Wrap Around X (Horizontal)        
        if (transform.position.x > Game.game.xBounds) //right side
            transform.position = new Vector3(-Game.game.xBounds + 0.5f, 1, transform.position.z);
        else if (transform.position.x < -Game.game.xBounds) //left side
            transform.position = new Vector3(Game.game.xBounds - 0.5f, 1, transform.position.z);

        //Wrap Around Z (Vertical)
        if (transform.position.z > Game.game.yBounds) //top side
            transform.position = new Vector3(transform.position.x, 1, -Game.game.yBounds + 0.5f);
        else if (transform.position.z < -Game.game.yBounds) //bottom side
            transform.position = new Vector3(transform.position.x, 1, Game.game.yBounds - 0.5f);


        if ((state == State.CAPTURING) &&
            ((destination == null) ||
             (!hasFlag &&
              (((gameObject.tag == "BlueTeam") && (Game.game.redFlag.carrier != null)) ||
               ((gameObject.tag == "RedTeam") && (Game.game.blueFlag.carrier != null))))))
            state = State.WANDERING;

        //NPC is close to its target, freeze target
        if ((state == State.PURSUING) && (target != null) &&
            ((transform.position - target.transform.position).magnitude <= 1))
            FreezeTarget(target);

        //NPC is close to ally, unfreeze target
        if ((state == State.UNFREEZE) && (target != null) &&
            ((transform.position - target.transform.position).magnitude <= 1))
            UnfreezeTarget(target);

        //If NPC has no target/pursuer and isn't doing anything
        if (pursuer == null && target == null && state != State.CAPTURING && state != State.FREEZE &&
            state != State.WANDERING)
            state = State.WANDERING;

        //If NPC is getting chased
        if ((pursuer != null) && ((transform.position - pursuer.transform.position).magnitude < 7.5f)) {
            //tried to capture flag
            if (state == State.CAPTURING && !hasFlag &&
                ((transform.position - pursuer.transform.position).magnitude < 3.5f)) {
                state = State.WANDERING;

                if (gameObject.tag == "BlueTeam") {
                    Game.game.redFlagTargeted = false;
                    Debug.Log("blue is scared");
                } else {
                    Game.game.blueFlagTargeted = false;
                    Debug.Log("red is scared");
                }
            }

            if (Game.game.aiBehaviour == Game.AIBehaviour.AI_BEHAVIOUR_1)
                KinematicFlee();
            else
                SteeringFlee();
        } else if ((state == State.CAPTURING) || (state == State.PURSUING) || (state == State.UNFREEZE)) {
            if (Game.game.aiBehaviour == Game.AIBehaviour.AI_BEHAVIOUR_1)
                KinematicArrive();
            else
                SteeringArrive();
        } else if (state == State.WANDERING) {
            Wander();
        }

        transform.position += velocity * Time.deltaTime;
    }

    public void SetToWander() {
        state = State.WANDERING;
        destination = null;
    }

    private void Wander() {
        if (Vector3.Angle(transform.forward, desiredDirection) < 2) {
            var random = Random.Range(-1f, 1f);
            rotation = random * 30;

            desiredDirection = Quaternion.AngleAxis(rotation, transform.up) * transform.forward;
        }

        Face(desiredDirection);

        velocity = transform.forward * moveSpeed;
    }

    public void Pursue(NPC target) {
        state = State.PURSUING;
        this.target = target;
        target.pursuer = this;
    }

    public void StopPursuing() {
        //Pursued target has no pursuer anymore
        if (target != null)
            target.pursuer = null;
        target = null;
        hasFlag = false;
    }

    //NPC freezes someone
    public void FreezeTarget(NPC target) {
        target.Freeze();
        StopPursuing();
        SetToWander();
    }

    //NPC unfreezes someone
    public void UnfreezeTarget(NPC target) {
        target.SetToWander();
        //npc.gameObject.GetComponent<Animation>().Play();        
        target.pursuer = null;
        StopPursuing();
        SetToWander();
    }

    //NPC freezes
    public void Freeze() {
        if (state == State.CAPTURING) {
            if (hasFlag) {
                hasFlag = false;
                if (gameObject.tag == "BlueTeam")
                    Game.game.RestoreRedFlag();
                else if (gameObject.tag == "RedTeam")
                    Game.game.RestoreBlueFlag();
            }

            if (gameObject.tag == "BlueTeam")
                Game.game.redFlagTargeted = false;
            else if (gameObject.tag == "RedTeam")
                Game.game.blueFlagTargeted = false;
        }

        state = State.FREEZE;
        velocity = Vector3.zero;
        StopPursuing();
    }

    //NPC unfreezes
    public void Unfreeze(NPC target) {
        state = State.UNFREEZE;
        this.target = target;
    }

    //Sets Flag as destination    
    public void CaptureFlag() {
        state = State.CAPTURING;

        if (gameObject.tag == "BlueTeam")
            destination = Game.game.redFlag.gameObject;
        else if (gameObject.tag == "RedTeam")
            destination = Game.game.blueFlag.gameObject;

        //Stops chasing NPC
        target = null;
    }

    private void Face(Vector3 dir) {
        //create the rotation we need to be in to look at the target
        var lookRotation = Quaternion.LookRotation(dir);
        var angle = Quaternion.Angle(transform.rotation, lookRotation);
        var timeToComplete = angle / 200f;
        var donePercentage = Mathf.Min(1F, Time.deltaTime / timeToComplete);

        //rotate towards direction with a specific amount per frame
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, donePercentage);
    }

    private Vector3 CalculateDirection() {
        if (state == State.PURSUING || (state == State.UNFREEZE && (target != null)))
            direction =
                (target.transform.position + target.speed * target.transform.forward * 20 - transform.position)
                    .normalized;

        else if ((state == State.CAPTURING) && hasFlag && (gameObject.tag == "BlueTeam"))
            destination = Game.game.blueFlagSpawn;

        else if ((state == State.CAPTURING) && hasFlag && (gameObject.tag == "RedTeam"))
            destination = Game.game.redFlagSpawn;

        if (state == State.CAPTURING)
            direction =
                new Vector3(destination.transform.position.x - transform.position.x, 0,
                    destination.transform.position.z - transform.position.z).normalized;

        if ((target != null) && (Math.Abs(transform.position.x - target.transform.position.x) > 35))
            direction = new Vector3(direction.x * -1, 0, direction.z);

        if ((target != null) && (Math.Abs(transform.position.z - target.transform.position.z) > 35))
            direction = new Vector3(direction.x, 0, direction.z * -1);

        //Speed calculation
        else if ((target != null) && ((target.transform.position - transform.position).magnitude <= nearRadius))
            speed = nearSpeed;

        else if ((state == State.CAPTURING) &&
                 ((destination.transform.position - transform.position).magnitude <= nearRadius))
            speed = nearSpeed;

        return direction;
    }

    private void KinematicArrive() {
        direction = CalculateDirection();
        if (speed <= nearSpeed) {
            //If small distance, speed is ok, just move
            //If larger distance, turn in place, then move
            if (((target != null) && ((target.transform.position - transform.position).magnitude > nearRadius)) ||
                ((state == State.CAPTURING) &&
                 ((destination.transform.position - transform.position).magnitude > nearRadius))) {
                Face(direction);

                if (Vector3.Angle(transform.forward, direction) <= 5)
                    speed = moveSpeed;
            }
        } else if (speed > nearSpeed) {
            if (Vector3.Angle(transform.forward, direction) <= 22.5)
                Face(direction);
            else
                speed = 0;
        }
        direction.y = transform.forward.y;
        velocity = direction * speed;
    }

    private void KinematicFlee() {
        if (!hasFlag) {
            SetToWander();

            if (gameObject.tag == "BlueTeam")
                Game.game.redFlagTargeted = false;
            else if (gameObject.tag == "RedTeam")
                Game.game.blueFlagTargeted = false;
        }

        var direction = (transform.position - pursuer.transform.position).normalized;

        //If small distance, speed is ok, just move
        //If larger distance, turn in place, then move
        if ((pursuer.transform.position - transform.position).magnitude > nearRadius) {
            speed = 0;

            Face(direction);

            if (Vector3.Angle(transform.forward, direction) <= 5)
                speed = moveSpeed + 3;
        }

        direction.y = transform.forward.y;
        velocity = direction * speed;
    }


    private void SteeringArrive() {
        direction = CalculateDirection();

        if (state == State.PURSUING)
            direction =
                (target.transform.position + target.speed * target.direction * 2 - transform.position).normalized;

        else if (state == State.UNFREEZE)
            direction = (target.transform.position - transform.position).normalized;

        var acceleration = Vector3.zero;

        float distance = 0;

        if ((state == State.PURSUING) || (state == State.UNFREEZE))
            distance = (target.transform.position - transform.position).magnitude;
        else if (state == State.CAPTURING)
            distance = (destination.transform.position - transform.position).magnitude;

        if (distance > slowRadius)
            speed = moveSpeed;
        else
            speed = moveSpeed * distance / slowRadius;

        Face(direction);

        if (speed <= nearSpeed) {
            //If small distance, speed is ok, just move
            //If larger distance, turn in place, then move
            if (((target != null) && ((target.transform.position - transform.position).magnitude > nearRadius)) ||
                ((state == State.CAPTURING) &&
                 ((destination.transform.position - transform.position).magnitude > nearRadius)))
                Face(direction);
        } else {
            if (Vector3.Angle(transform.forward, direction) <= 22.5)
                Face(direction);
        }

        acceleration = maxAcceleration * direction;

        velocity += acceleration * Time.deltaTime;

        if (velocity.magnitude > moveSpeed) {
            velocity.Normalize();
            velocity *= moveSpeed;
        }
    }

    private void SteeringFlee() {
        Vector3 acceleration = Vector3.zero;

        if (!hasFlag) {
            SetToWander();

            if (gameObject.tag == "BlueTeam")
                Game.game.redFlagTargeted = false;
            else if (gameObject.tag == "RedTeam")
                Game.game.blueFlagTargeted = false;
        }

        direction =
            (transform.position - (pursuer.transform.position + pursuer.speed * pursuer.direction * 2)).normalized;

        if ((transform.position - pursuer.transform.position).magnitude >= 20) {
            Face(direction);

            acceleration = maxAcceleration * direction;

            velocity += acceleration * Time.deltaTime;

            if (velocity.magnitude > moveSpeed + 3) {
                velocity.Normalize();
                velocity *= moveSpeed + 3;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        //if NPC is in home territory and not frozen, and hits other team NPC       
        if (isHome && (state != State.FREEZE) &&
            (((gameObject.tag == "BlueTeam") && (other.gameObject.tag == "RedTeam")) ||
             ((gameObject.tag == "RedTeam") && (other.gameObject.tag == "BlueTeam")))) {
            //Freeze other team NPC that is in home territory
            if (other.gameObject.GetComponent<NPC>().state != State.FREEZE) {
                FreezeTarget(other.gameObject.GetComponent<NPC>());

                //Other NPC stops pursuing frozen NPC
                if (other.gameObject.GetComponent<NPC>().pursuer != null) {
                    other.gameObject.GetComponent<NPC>().pursuer.StopPursuing();
                    other.gameObject.GetComponent<NPC>().pursuer = null;
                }
            }
        }

        //if NPC is not frozen and hits ally NPC
        if ((state != State.FREEZE) &&
            (((gameObject.tag == "BlueTeam") && (other.gameObject.tag == "BlueTeam")) ||
             ((gameObject.tag == "RedTeam") && (other.gameObject.tag == "RedTeam")))) {
            //Unfreeze ally NPC
            if (other.gameObject.GetComponent<NPC>().state == State.FREEZE) {
                UnfreezeTarget(other.gameObject.GetComponent<NPC>());
            }
        }

        //if NPC touches flag, hasFlag = other ally has not obtained flag yet, i.e. flag not in original position
        if (state == State.CAPTURING && gameObject.tag == "BlueTeam" && other.gameObject.tag == "RedFlag" && !hasFlag) {
            hasFlag = true;
            Game.game.redFlag.carrier = this;
        }

        if (state == State.CAPTURING && gameObject.tag == "RedTeam" && other.gameObject.tag == "BlueFlag" && !hasFlag) {
            hasFlag = true;
            Game.game.blueFlag.carrier = this;
        }
        //if NPC enters other team's home territory
        //if not capturing, then start wandering and stop every pursue
        if ((gameObject.tag == "BlueTeam" && other.gameObject.tag == "RedHome") ||
            (gameObject.tag == "RedTeam" && other.gameObject.tag == "BlueHome")) {
            isHome = false;
            if (state == State.CAPTURING)
                return;
            StopPursuing();
            SetToWander();
        }
        //if NPC enters home territory
        if (((gameObject.tag == "BlueTeam") && (other.gameObject.tag == "BlueHome")) ||
            ((gameObject.tag == "RedTeam") && (other.gameObject.tag == "RedHome"))) {
            isHome = true;
            //pursuer stops chasing
            if (pursuer != null)
                pursuer.StopPursuing();
        }

        if (((gameObject.tag == "BlueTeam") && (other.gameObject.tag == "BlueFlagSpawn")) ||
           ((gameObject.tag == "RedTeam") && (other.gameObject.tag == "RedFlagSpawn"))) {
            if (hasFlag && gameObject.tag == "BlueTeam") {
                hasFlag = false;
                Game.game.ScoreBlue();
            }
            if (hasFlag && (gameObject.tag == "RedTeam")) {
                hasFlag = false;
                Game.game.ScoreRed();
            }
        }
    }
}
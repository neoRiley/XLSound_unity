
using System.Collections.Generic;
using UnityEngine;

class VelocityTracker
{

    public Vector3 CurrentVel { get; private set; }
    public List<Position> positions = new List<Position>();
    public float sampleDuration = .15f;

    public void Sample(Vector3 currentPosition)
    {
        CurrentVel = Vector3.zero;
        positions.Add(new Position { pos=currentPosition, time=Time.time });
        for (var i = positions.Count; i-- > 0;)
        {
            var pos = positions[i];
            //Calculate Current velocity
            if (CurrentVel == Vector3.zero && Time.time - pos.time > sampleDuration)
            {
                CurrentVel = (currentPosition - pos.pos) / sampleDuration;
            }
            //Remove old positions
            if (Time.time - positions[i].time > 1)
            {
                positions.RemoveAt(i);
            }
        }
    }

    public struct Position
    {
        public float time;
        public Vector3 pos;
    }
}
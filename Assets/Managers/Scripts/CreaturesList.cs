using System;
using System.Collections.Generic;

[Serializable]
public class CreaturesList
{
    public List<Creature> list;

    public CreaturesList(List<Creature> list)
    {
        this.list = list;
    }
}
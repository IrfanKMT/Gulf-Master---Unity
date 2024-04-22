using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum MatchType
{
    None,
    TwoPlayer,
    FourPlayer
}

public enum TeamType
{
    None,
    TeamA,
    TeamB
}

[System.Serializable]
public class TeamAssign
{
    public GamePlayer Player;
    public TeamType TeamType;
}

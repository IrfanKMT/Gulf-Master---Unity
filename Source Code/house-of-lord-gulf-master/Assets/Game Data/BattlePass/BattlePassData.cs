using System.Collections.Generic;
using System;

public class BattlePassData
{
    public List<Season> seasons;   
}

[Serializable]
public class Season
{
    public int endYear;
    public int endMonth;
    public int endDate;
    public List<Reward> rewards;
}

[Serializable]
public class Reward
{
    public int level;
    public string rewardImageURL;
    public List<string> rewardID;
}

using UnityEngine;

public static class PlayerData
{
    public static string Email {
        get { return PlayerPrefs.GetString("EMAIL"); }
        set
        {
            PlayerPrefs.SetString("EMAIL", value);
            PlayerPrefs.DeleteKey("CUSTOMID");
        }
    }
    public static string Password { get { return PlayerPrefs.GetString("PASSWORD"); } set { PlayerPrefs.SetString("PASSWORD", value); } }

    public static string Username
    {
        get
        {
            return PlayerPrefs.GetString("USERNAME");
        }

        set
        {
            string username = value[0..^4] + "#" + value.Remove(0, value.Length - 4);
            PlayerPrefs.SetString("USERNAME", username);
        }
    }

    public static string PlayfabID { get { return PlayerPrefs.GetString("PLAYFABID"); } set { PlayerPrefs.SetString("PLAYFABID", value); } }

    public static int Gender { get { return PlayerPrefs.GetInt("GENDER"); } set { PlayerPrefs.SetInt("GENDER", value); } }
    public static int Country { get { return PlayerPrefs.GetInt("COUNTRY"); } set { PlayerPrefs.SetInt("COUNTRY", value); } }
    public static int Age { get { return PlayerPrefs.GetInt("AGE",5); } set { PlayerPrefs.SetInt("AGE", value); } }


    public static string BoosterID { get { return PlayerPrefs.GetString("BOOSTERID"); } set { PlayerPrefs.SetString("BOOSTERID", value); } }
    public static string Perk1ID { get { return PlayerPrefs.GetString("PERK1ID"); } set { PlayerPrefs.SetString("PERK1ID", value); } }
    public static string Perk2ID { get { return PlayerPrefs.GetString("PERK2ID"); } set { PlayerPrefs.SetString("PERK2ID", value); } }

    public static void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
    }
}

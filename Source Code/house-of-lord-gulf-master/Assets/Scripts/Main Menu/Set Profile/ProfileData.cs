[System.Serializable]
public class ProfileData
{
    public string UserName;    
    public int Age;
    public Gender Gender;
    public Countries Country;

    public ProfileData(string username, int age, int gender, int country)
    {
        UserName = username;
        Age = age;
        Gender = (Gender)gender;
        Country = (Countries)country;
    }
}

[System.Serializable]
public enum Countries : int
{
    None,
    Bahrain,
    Kuwait,
    UAE,
    Saudi,
    Qatar,
    Oman
}

[System.Serializable]
public enum Gender : int
{
    None,
    Male,
    Female
}
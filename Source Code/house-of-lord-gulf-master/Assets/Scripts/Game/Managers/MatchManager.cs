using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

public class MatchManager : MonoBehaviour
{
    public DatabaseReference DBref;


    void Start()
    {
        DBref = FirebaseDatabase.DefaultInstance.RootReference;
    }

    //Used to fetch matchHistory of the user , to be done each login or whenever you feel necessary
    public void OnLogin(string PlayerUsernameOrID)
    {
        //Fetching Match History
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;

        dbInstance.GetReference("Players").Child(PlayerUsernameOrID).Child("Matches").GetValueAsync().ContinueWithOnMainThread(DBTask =>
       {
           if (DBTask.IsFaulted)
           {
               Debug.LogWarning(DBTask.Exception);
               Debug.Log("Exception");
           }

           else if (DBTask.IsCompleted)
           {
               DataSnapshot snapshot = DBTask.Result;
               //Modify it to your use case accordingly , currently it lists who won in which match
               foreach (DataSnapshot Matches in snapshot.Children)
               {
                   Debug.Log("Opponent: " + Matches.Key);
                   Debug.Log("Did the user(PlayerUsernameOrID) Win?: " + Matches.Child("Won").Value.ToString());
               }
           }
       });
    }


    //call this function when the match has started
    //To be called by both players
    public void EnterMatch(string OpponentUsernameOrID, string PlayerUsernameOrID)
    {
        var DBTask = DBref.Child("Players").Child(PlayerUsernameOrID).Child("Matches").Child(OpponentUsernameOrID).Child("Ongoing").SetValueAsync(true);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning(DBTask.Exception);
        }
    }

    //Call this function when the match is over , call it only for the person who wins , the losing party doesnt have to call this function
    //The Person who wins writes the loss of the losing person
    public void OnMatchWon(string OpponentUsernameOrID, string PlayerUsernameOrID)
    {
        DatabaseReference playerRef = DBref.Child("Players").Child(PlayerUsernameOrID).Child("Matches").Child(OpponentUsernameOrID);
        DatabaseReference opponentRef = DBref.Child("Players").Child(OpponentUsernameOrID).Child("Matches").Child(PlayerUsernameOrID);

        var DBTask1 = playerRef.Child("Ongoing").SetValueAsync(false);
        var DBTask2 = opponentRef.Child("Ongoing").SetValueAsync(false);

        var DBTask3 = playerRef.Child("Won").SetValueAsync(true);
        var DBTask4 = opponentRef.Child("Won").SetValueAsync(false);

        if (DBTask1.Exception != null)
        {
            Debug.LogWarning(DBTask1.Exception);
        }

        if (DBTask2.Exception != null)
        {
            Debug.LogWarning(DBTask2.Exception);
        }

        if (DBTask3.Exception != null)
        {
            Debug.LogWarning(DBTask3.Exception);
        }

        if (DBTask4.Exception != null)
        {
            Debug.LogWarning(DBTask4.Exception);
        }
    }


}

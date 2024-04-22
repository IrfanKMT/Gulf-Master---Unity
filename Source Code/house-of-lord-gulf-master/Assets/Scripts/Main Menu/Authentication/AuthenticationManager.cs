using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using System;
using System.Collections.Generic;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager manager;
    public event Action OnPlayerLoggedIn;
    public event Action OnPlayerLoggedOut;

    [SerializeField] AuthenticationUIManager uiManager;

    [Header("Guest")]
    [SerializeField] TMP_InputField guest_name_tf;
    [SerializeField] TMP_Text guest_error_txt;

    [Header("Set Name")]
    [SerializeField] GameObject setNamePanel;
    [SerializeField] Button setNameButton;
    [SerializeField] TMP_Text setName_error_txt;

    [Header("Sign In")]
    [SerializeField] TMP_InputField signIn_email_tf;
    [SerializeField] TMP_InputField signIn_pass_tf;
    [SerializeField] TMP_Text signIn_error_txt;

    [Header("Sign Up")]
    [SerializeField] TMP_InputField signUp_username_tf;
    [SerializeField] TMP_InputField signUp_email_tf;
    [SerializeField] TMP_InputField signUp_pass_tf;
    [SerializeField] TMP_InputField signUp_confirm_pass_tf;
    [SerializeField] TMP_Text signUp_error_txt;

    [Header("Forgot Password")]
    [SerializeField] TMP_InputField forgot_password_email_tf;
    [SerializeField] TMP_Text forgot_password_error_txt;

    [Header("Link Account")]
    [SerializeField] TMP_InputField link_acc_emailTF;
    [SerializeField] TMP_InputField link_acc_passTF;
    [SerializeField] TMP_Text link_acc_error_txt;
    [SerializeField] GameObject link_acc_accountLinkedGO;

    public bool LoggedIn { get; private set; }
    internal string entityID;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    #endregion

    #region Initialize

    public void StartLoginProcess()
    {
        string email = PlayerData.Email;
        string pass = PlayerData.Password;

        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(pass))
        {
            Debug.Log("Login");
            Login(email, pass);
        }
        else
        {
            Debug.Log("GuestLogin");
            GuestLogin();
            //We are only using Guest Login rn
            //UIManager.manager.lobbyUI.SetActive(false);
        }
    }

    #endregion

    #region Guest

    private void GuestLogin()
    {
        LoadingManager.manager.UpdateLoadingText("Signing you as a guest...");

        var InfoRequestParams = new GetPlayerCombinedInfoRequestParams
        {
            GetPlayerProfile = true,
            GetUserData = true,
            GetUserAccountInfo = true
        };

        var loginAsGuestReq = new LoginWithCustomIDRequest
        {
            CreateAccount = true,
            TitleId = "4E495",
            InfoRequestParameters = InfoRequestParams,
            CustomId = SystemInfo.deviceUniqueIdentifier.Length > 30 ? SystemInfo.deviceUniqueIdentifier.Substring(0, 30) : SystemInfo.deviceUniqueIdentifier
            //CustomId = System.Guid.NewGuid().ToString()
        };

        PlayFabClientAPI.LoginWithCustomID(loginAsGuestReq, OnGuestAccountLoggedIn, OnGuestAccountLoginError);
    }

    private void OnGuestAccountLoggedIn(LoginResult result)
    {
        PlayerData.PlayfabID = result.PlayFabId;

        if (result.InfoResultPayload.PlayerProfile==null || string.IsNullOrEmpty(result.InfoResultPayload.PlayerProfile.DisplayName))
            UIAnimationManager.manager.PopUpPanel(setNamePanel, DG.Tweening.Ease.OutBounce);
        else
            PlayerData.Username = result.InfoResultPayload.PlayerProfile.DisplayName;

        var titleData = result.InfoResultPayload.UserData;
        ValidateProfile(titleData, result);
    }

    private void Guest_SetName()
    {
        setNameButton.interactable = false;
        var updateDisplayNameRequest = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = guest_name_tf.text
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(updateDisplayNameRequest, (res) =>
        {
            PlayerData.Username = res.DisplayName;
            UIAnimationManager.manager.PopDownPanel(setNamePanel, DG.Tweening.Ease.InBack);
        },
        (err)=>
        {
            setName_error_txt.text = err.ErrorMessage;
            setNameButton.interactable = true;
            Debug.LogError("Error Setting Display Name\nError :" + err.GenerateErrorReport());
        });
    }

    private void OnGuestAccountLoginError(PlayFabError error)
    {
        guest_error_txt.text = error.ErrorMessage;
        Debug.LogError("Authentication Error :\nGuest Account Login Error : " + error.GenerateErrorReport());
        LoadingManager.manager.HideLoadingBar();
    }

    #endregion

    #region Login

    private void Login(string email, string pass)
    {
        LoadingManager.manager.UpdateLoadingText("Logging in...");

        var InfoRequestParams = new GetPlayerCombinedInfoRequestParams
        {
            GetPlayerProfile = true,
            GetUserData = true
        };

        var loginRequest = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = pass,
            InfoRequestParameters = InfoRequestParams
        };

        PlayFabClientAPI.LoginWithEmailAddress(loginRequest,
        (res) =>
        {
            PlayerData.Email = email;
            PlayerData.Password = pass;
            PlayerData.Username = res.InfoResultPayload.PlayerProfile.DisplayName;
            OnLoginSuccess(res);
        }
        , OnLoginError);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        PlayerData.PlayfabID = result.PlayFabId;

        Debug.Log(result.PlayFabId);
        var titleData = result.InfoResultPayload.UserData;
        ValidateProfile(titleData, result);
    }

    private void OnLoginError(PlayFabError error)
    {
        LoadingManager.manager.HideLoadingBar();
        ShowError(signIn_error_txt, error);
    }

    #endregion

    #region Register

    private void Register(string email, string pass, string username)
    {
        LoadingManager.manager.UpdateLoadingText("Registering your account ...");

        var registerRequest = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = pass,
            DisplayName = username,
            RequireBothUsernameAndEmail = false,
            InfoRequestParameters = new()
            {
                GetTitleData = true
            }
        };

        PlayFabClientAPI.RegisterPlayFabUser(registerRequest,
        (res)=>
        {
            PlayerData.Email = email;
            PlayerData.Password = pass;
            entityID = res.EntityToken.Entity.Id;
            OnRegisterSuccess(res);
        },
        OnRegisterError);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log("Registration Successful");
        PlayerData.PlayfabID = result.PlayFabId;

        var getDisplayNameRequest = new GetAccountInfoRequest
        {
            PlayFabId = result.PlayFabId,
        };

        PlayFabClientAPI.GetAccountInfo(getDisplayNameRequest,
            (res) =>
            {
                PlayerData.Username = res.AccountInfo.TitleInfo.DisplayName;
                LoadingManager.manager.HideLoadingBar(() => UIManager.manager.OpenPanel(UIManager.manager.setPlayerProfilePanel, UIManager.manager.authenticationPanel));
            },
            OnRegisterError);

    }

    private void OnRegisterError(PlayFabError error)
    {
        LoadingManager.manager.HideLoadingBar();
        ShowError(signUp_error_txt, error);
    }

    #endregion

    #region Link Account

    public void LinkAccount()
    {
        string email = link_acc_emailTF.text;
        string pass = link_acc_passTF.text;

        if (!ValidateEmailAndPass(email, pass, out string errorText))
            ShowError(link_acc_error_txt, errorText);
        else
        {
            LoadingManager.manager.UpdateLoadingText("Linking Your Guest Account");

            var getDisplayNameReq = new GetAccountInfoRequest
            {
                PlayFabId = PlayerData.PlayfabID
            };
            PlayFabClientAPI.GetAccountInfo(getDisplayNameReq, LinkAccount_OnAccountInfoRecieved, LinkAccount_OnError);
        }
    }

    private void LinkAccount_OnAccountInfoRecieved(GetAccountInfoResult result)
    {
        var linkUserAccountReq = new AddUsernamePasswordRequest
        {
            Username = result.AccountInfo.TitleInfo.DisplayName,
            Email = link_acc_emailTF.text,
            Password = link_acc_passTF.text
        };
        PlayFabClientAPI.AddUsernamePassword(linkUserAccountReq, LinkAccount_OnEmailPassAdded, LinkAccount_OnError);
    }

    private void LinkAccount_OnEmailPassAdded(AddUsernamePasswordResult result)
    {
        LoadingManager.manager.HideLoadingBar(()=>
        {
            UIAnimationManager.manager.PopUpPanel(link_acc_accountLinkedGO, DG.Tweening.Ease.OutBounce);
        });
    }

    private void LinkAccount_OnError(PlayFabError error)
    {
        link_acc_error_txt.text = error.ErrorMessage;
        Debug.LogError("Linking Account Error : \nError :" + error.GenerateErrorReport());
        LoadingManager.manager.HideLoadingBar();
    }

    #endregion

    #region Logout

    public void Logout()
    {
        LoggedIn = false;
        OnPlayerLoggedOut?.Invoke();
        PlayerData.ClearAllData();
    }

    #endregion

    #region Common

    private void ValidateProfile(Dictionary<string,UserDataRecord> titleData, LoginResult result)
    {
        entityID = result.EntityToken.Entity.Id;

        if (titleData.ContainsKey(PlayfabDataKeys.PlayerProfile) && SetPlayerProfileManager.manager.IsPlayerProfileCorrect(titleData[PlayfabDataKeys.PlayerProfile].Value))
            TriggerOnLoggedInEvent();
        else
            LoadingManager.manager.HideLoadingBar(() => UIManager.manager.OpenPanel(UIManager.manager.setPlayerProfilePanel, UIManager.manager.authenticationPanel));
    }

    #endregion

    #region Forgot Password

    private void ForgotPassword(string email)
    {
        var sendRecoveryEmailRequest = new SendAccountRecoveryEmailRequest
        {
            Email = email,
            TitleId = "4E495"
        };

        PlayFabClientAPI.SendAccountRecoveryEmail(sendRecoveryEmailRequest, OnAccoutnRecoveryEmailSent, OnAccoutnRecoveryEmailError);
    }

    private void OnAccoutnRecoveryEmailSent(SendAccountRecoveryEmailResult result)
    {
        uiManager.Open_AccountRecoveryEmailSentPanel();
    }

    private void OnAccoutnRecoveryEmailError(PlayFabError error)
    {
        ShowError(forgot_password_error_txt, error);
    }

    #endregion

    #region Button Callback

    public void OnClick_SignIn()
    {
        string email = signIn_email_tf.text;
        string pass = signIn_pass_tf.text;


        if (!ValidateEmailAndPass(email, pass, out string errorText))
            ShowError(signUp_error_txt, errorText);
        else
            Login(email, pass);
    }

    public void OnClick_SignUp()
    {
        string username = signUp_username_tf.text;
        string email = signUp_email_tf.text;
        string pass = signUp_pass_tf.text;
        string confirm_pass = signUp_confirm_pass_tf.text;

        if (!pass.Equals(confirm_pass))
        {
            ShowError(signUp_error_txt,"Password didnt match");
            return;
        }

        if (string.IsNullOrEmpty(username))
            ShowError(signUp_error_txt, "Username is empty");

        if (!ValidateEmailAndPass(email, pass, out string errorText))
            ShowError(signUp_error_txt, errorText);
        else
            Register(email, pass, username);
        
    }

    public void OnClick_ForgetPassword()
    {
        string email = forgot_password_email_tf.text;

        if (!string.IsNullOrEmpty(email))
            ForgotPassword(email);
        else
            ShowError(forgot_password_error_txt, "Please enter a valid email");

    }

    public void OnClick_SetNameBtn()
    {
        string username = guest_name_tf.text;

        if(username.Contains(" "))
        {
            guest_error_txt.text = "Name should not contain whitespace";
            return;
        }

        if(username.Length>=3 && username.Length <= 25)
        {
            Guest_SetName();
        }
        else
        {
            guest_error_txt.text = "Name should be between 3 and 25 characters";
        }
    }

    public void OnClick_Guest()
    {
        GuestLogin();
    }

    #endregion

    #region Helper Functions

    public void TriggerOnLoggedInEvent()
    {
        print("Logged Into Playfab");

        LoggedIn = true;

        OnPlayerLoggedIn?.Invoke();
    }

    private bool ValidateEmailAndPass(string email, string pass, out string text)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            text = "Please enter your email and password";
            return false;
        }
        else if (pass.Length < 6 || pass.Length > 100)
        {
            text = "Password should be between 6 and 100 characters";
            return false;
        }
        else if(!email.Contains("@") && !email.Contains("."))
        {
            text = "Please enter a valid email";
            return false;
        }

        text = "";
        return true;
    }

    private void ShowError(TMP_Text text, PlayFabError error)
    {
        Debug.LogError("Authentication Error : \n" + error.GenerateErrorReport());
        StopAllCoroutines();
        StartCoroutine(ShowError_Coroutine(text, error.ErrorMessage, 7));
    }

    private void ShowError(TMP_Text text, string error)
    {
        StartCoroutine(ShowError_Coroutine(text, error, 2));
    }

    IEnumerator ShowError_Coroutine(TMP_Text text, string errorMsg, int secs)
    {
        text.text = "";
        yield return new WaitForSeconds(0.1f);
        text.text = errorMsg;
        yield return new WaitForSeconds(secs);
        text.text = "";
    }

    #endregion
}
using UnityEngine;
using Thinksquirrel.CShake;
using Lofelt.NiceVibrations;
using Mirror;

public class ClearablePiece : MonoBehaviour
{
    public ParticleSystem destroyEffect;
    [Space]
    //public AudioClip[] destroyCandySFXs;

    protected bool isBeingCleared = false;
    public bool IsBeingCleared
    {
        get { return isBeingCleared; }
    }

    public GamePiece piece;
    bool hapticsSupported = DeviceCapabilities.isVersionSupported;

    //public virtual void Awake()
    //{
    //    piece = GetComponent<GamePiece>();
    //}

    public void ClearPiece(PieceType clearingPieceType = PieceType.NORMAL)
    {
        if (isBeingCleared) return;
        isBeingCleared = true;

#if !UNITY_SERVER
        AddCameraShake();
        SoundManager.manager.PlaySoundForCandyDestructionSapratly();
#endif
        Clear(clearingPieceType);
    }

    private void AddCameraShake()
    {

        if (piece.Type != PieceType.NORMAL)
        {
            if (piece.Type == PieceType.COLUMN_CLEAR || piece.Type == PieceType.ROW_CLEAR)
            {
                CameraShake.shake.shakeAmount = CameraShake.shake.m_ShakeAmountRocket;

                if (hapticsSupported)
                    HapticPatterns.PlayConstant(1f, 0.0f, 0.25f);
            }
            else if (piece.Type == PieceType.ELECTRIC)
            {
                CameraShake.shake.shakeAmount = CameraShake.shake.m_ShakeAmountElectric;

                if (hapticsSupported)
                    HapticPatterns.PlayConstant(1f, 0.0f, 0.5f);
            }
            else if (piece.Type == PieceType.BOMB)
            {
                CameraShake.shake.shakeAmount = CameraShake.shake.m_ShakeAmountBomb;

                if (hapticsSupported)
                    HapticPatterns.PlayConstant(1f, 0.0f, 0.7f);
            }

            CameraShake.ShakeAll();
        }
    }

    protected virtual void Clear(PieceType clearingPieceType)
    {
        GamePlayManager.manager.Server_AddScore();

        if (destroyEffect != null && clearingPieceType==PieceType.NORMAL )
            Instantiate(destroyEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}

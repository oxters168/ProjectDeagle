using UnityEngine;

namespace ProjectDeagle
{
    public class WeaponResource
    {
        public EquipmentElement equipmentElement;

        #region Vectors
        public Vector3 position { get; internal set; } //m_vecOrigin
        public Vector3 rotation { get; internal set; } //m_angRotation
        #endregion

        #region Floats
        public float wear { get; internal set; } //m_flFallbackWear
        #endregion

        #region Integers
        public int skin { get; internal set; } //m_nSkin
        public int paintKit { get; internal set; } //m_nFallbackPaintKit
        public int seed { get; internal set; } //m_nFallbackSeed
        public int stattrak { get; internal set; } //m_nFallbackStatTrak

        public int clip1 { get; internal set; } //m_iClip1
        public int primaryAmmoReserve { get; internal set; } //m_iPrimaryReserveAmmoCount
        public int primaryAmmoType { get; internal set; } //LocalWeaponData.m_iPrimaryAmmoType

        public int muzzleFlashParity { get; internal set; } //m_nMuzzleFlashParity

        public int viewModelIndex { get; internal set; } //m_iViewModelIndex
        public int worldModelIndex { get; internal set; } //m_iWorldModelIndex

        public int owner { get; internal set; } //m_hOwner
        public int previousOwner { get; internal set; } //m_hPrevOwner

        public int zoomLevel { get; internal set; } //m_zoomLevel

        public int state { get; internal set; } //m_iState
        #endregion

        #region Booleans
        public bool burstMode { get; internal set; } //m_bBurstMode
        public bool silenced { get; internal set; } //m_bSilencerOn
        #endregion

        #region Strings
        public string customName { get; internal set; } //m_AttributeManager.m_Item.m_szCustomName
        #endregion

        internal WeaponResource() { }
        internal WeaponResource(WeaponResource other)
        {
            equipmentElement = other.equipmentElement;

            #region Vectors
            position = other.position;
            rotation = other.rotation;
            #endregion

            #region Floats
            wear = other.wear;
            #endregion

            #region Integers
            skin = other.skin;
            paintKit = other.paintKit;
            seed = other.seed;
            stattrak = other.stattrak;

            clip1 = other.clip1;
            primaryAmmoReserve = other.primaryAmmoReserve;
            primaryAmmoType = other.primaryAmmoType;

            muzzleFlashParity = other.muzzleFlashParity;

            viewModelIndex = other.viewModelIndex;
            worldModelIndex = other.worldModelIndex;

            owner = other.owner;
            previousOwner = other.previousOwner;

            zoomLevel = other.zoomLevel;

            state = other.state;
            #endregion

            #region Booleans
            burstMode = other.burstMode;
            silenced = other.silenced;
            #endregion

            #region Strings
            customName = other.customName;
            #endregion
        }
    }
}
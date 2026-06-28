using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Static access point for combat tuning. ProgressionEngine and BattleManager read from Active.
    /// Assign a CombatBalanceSO on BattleManager, or create one via Tools → Fighter Tools → Create Combat Balance Asset.
    /// </summary>
    public static class CombatBalance
    {
        private static CombatBalanceSO _active;

        /// <summary>
        /// Current balance data. Uses SetActive override, then Resources/CombatBalance, then baked-in defaults.
        /// </summary>
        public static CombatBalanceSO Active
        {
            get
            {
                if (_active != null)
                {
                    return _active;
                }

                _active = Resources.Load<CombatBalanceSO>("CombatBalance");
                if (_active != null)
                {
                    return _active;
                }

                _active = CombatBalanceSO.CreateWithDefaults();
                return _active;
            }
        }

        /// <summary>
        /// Override the active balance (e.g. BattleManager assigns its Inspector reference on startup).
        /// </summary>
        public static void SetActive(CombatBalanceSO balance)
        {
            _active = balance;
        }
    }
}

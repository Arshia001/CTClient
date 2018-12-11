using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class CurrencyHelpers
{
    public static bool HaveEnoughCurrency(int Amount, CurrencyType Type)
    {
        switch (Type)
        {
            case CurrencyType.Gem:
                return TransientData.Instance.UserProfile.Gems >= (ulong)Amount;
            case CurrencyType.Gold:
                return TransientData.Instance.UserProfile.Gold >= (ulong)Amount;
            default:
                return false;
        }
    }

    public static ulong GetLacking(int Amount, CurrencyType Type)
    {
        switch (Type)
        {
            case CurrencyType.Gem:
                return (ulong)Amount - TransientData.Instance.UserProfile.Gems;
            case CurrencyType.Gold:
                return (ulong)Amount - TransientData.Instance.UserProfile.Gold;
            default:
                return 0;
        }
    }
}

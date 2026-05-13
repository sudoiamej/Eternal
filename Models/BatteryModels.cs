using System;

namespace Eternal.Models
{
    public enum ChargingState { Discharging, Charging, Full, Bypass, Unknown }

    public record BatteryInfo(
        string Status,
        int ChargeLevel,
        string PowerSource,
        double WearLevel,
        int DesignCapacity,
        int FullChargeCapacity,
        int CurrentCapacity,
        int CycleCount,
        string Chemistry,
        string DeviceName,
        double Temperature,
        double Voltage,
        double ChargeRateWattage,
        TimeSpan EstimatedTimeRemaining,
        ChargingState ChargingState
    );
}

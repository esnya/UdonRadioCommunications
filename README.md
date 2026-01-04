## **Archived**

Since August 2022, I parted ways with a certain company under unfavorable circumstances. Since I am no longer active on that platform, this repository can no longer be properly maintained and has been archived.

For an actively maintained alternative, please see:
https://github.com/VirtualAviationJapan/UdonRadioCommunications-Redux

# UdonRadioCommunication
Simplified radio communication system for VRChat Udon worlds.

![image](https://user-images.githubusercontent.com/2088693/219715229-396f0e71-921a-4e2e-814a-d814944c3fe8.png)

## Getting Started
1. Create a Unity Project for VRChat World with UdonSharp using VRChat Creator Compoanion
2. Open the Unity Project.
3. Open the Package Manager window from Window menu.
4. Click + button and select `Add package from git URL`.
5. Enter `git+https://github.com/esnya/UdonRadioCommunications.git?path=/Packages/com.nekometer.esnya.udon-radio-communications` and click Add button (Enter `git+https://github.com/esnya/UdonRadioCommunications.git?path=/Packages/com.nekometer.esnya.udon-radio-communications#beta` to use beta releases)
6. Install [optional dependencies](#optional-dependencies) if you need.

## Usage
- Place `Transmitter`s and `Receiver`s wherever you want.
- Call custom events `Activate` and `Deactivate` and set variable `frequency` by player interactions.
- Add a single `UdonRadioCommunication` to the scene.

For more usage such as `Transceiver`, open a scene `Demo.unity`.

## Optional Dependencies
| Name | Description |
| :-- | :-- |
 |[InariUdon](https://github.com/esnya/InariUdon.git) | `Interaction/TouchSwitch` and `Interaction/KeyboardInput` are used and **required in sample prefabs**.  |

## Runtime Overhead
Only one udon is using the `Update` loop. If the number of `Transmitters` is `Nt`, the number of `Receivers` is `Nr`, and the number of `Players` is `Np`, the computational complexity is `O(Np(Nt+Nr))`.

## Configurations

### Transceiver
| Property Name | Description |
| :-- | :-- |
| Exclusive | Turn off receiver during transmitting. |


### Receiver
| Property Name | Description |
| :-- | :-- |
| Sync | If checked, anyone who is near the receiver can listen to the radio. If not, only local clients. |

# SaccFlight Integrations
Integration addons for SaccFlightAndVehicles. DFUNCs are provided to manage fequency, toggle receiving and push to talk.

![image](https://user-images.githubusercontent.com/2088693/219712019-99885e55-98cc-4578-8931-456da063de62.png)

## Installation
1. Open the Package Manager window from Window menu.
2. Click + button and select `Add package from git URL`.
3. Enter `git+https://github.com/esnya/UdonRadioCommunications.git?path=/Packages/com.nekometer.esnya.udon-radio-communications-sf` and click Add button (Enter `git+https://github.com/esnya/UdonRadioCommunications.git?path=/Packages/com.nekometer.esnya.udon-radio-communications-sf#beta` to use beta releases)

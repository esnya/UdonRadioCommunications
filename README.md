# UdonRadioCommunication
Simplified radio communication system for VRChat Udon worlds.

## Getting Started
1. Create a Unity Project for VRChat World with UdonSharp using VRChat Creator Compoanion
2. Open the Unity Project.
3. Open the Package Manager window from Window menu.
4. Click + button and select `Add package from git URL`.
5. Enter `git+https://github.com/esnya/UdonRadioCommunications.git?pth=/Packages/com.nekometer.esnya.udon-radio-communications` and click Add button (Enter `git+https://github.com/esnya/UdonRadioCommunications.git?pth=/Packages/com.nekometer.esnya.udon-radio-communications#beta` to use beta releases)

## Usage
- Place `Transmitter`s and `Receiver`s wherever you want.
- Call custom events `Activate` and `Deactivate` and set variable `frequency` by player interactions.
- Add a single `UdonRadioCommunication` to the scene.
- Press "Setup" button in the inspector.
  - Press again when you added or removed `Transmitter`s or `Receiver`s.

For more usage such as `Transceiver`, open a scene `Demo.unity`.

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

## Integrations
- [SaccFlightAndVehicles (1.6)](Packages/com.nekometer.esnya.udon-radio-communications-sf)

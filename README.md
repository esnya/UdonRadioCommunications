# UdonRadioCommunication
Simplified radio communication system for VRChat Udon worlds.

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

## Integrations
- [SaccFlightAndVehicles (1.5)](https://github.com/esnya/UdonRadioCommunication/tree/master/Assets/UdonRadioCommunication/Integrations/SaccFlightAndVehicles#readme)

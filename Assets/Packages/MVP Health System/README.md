# MVP Health System

A health system with MVP architecture providing health models, health bar views, hit box presenters, and healable components. Integrates with the Event Bus for damage/heal events and the UI Management System for health bar display.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| Event Bus | `com.mytoolz.eventbus` |
| MVP UI Management System | `com.mytoolz.mvpuimanagementsystem` |

External: Zenject, DOTween (`DG.Tweening`).

## Structure

```
Runtime/
├── Interfaces.cs                     IDamageable, IHealable, IHealthSystem contracts
├── DamageTypes.cs                    Damage type definitions
├── Models/
│   └── HealthSystemModel.cs          Health data model with current/max HP
├── Installers/
│   └── HealthSystemInstaller.cs      Zenject installer
├── Presenters/
│   ├── HealthSystemPresenter.cs      Main health presenter
│   ├── HitBoxPresenter.cs            Collider-based damage receiver
│   └── HealableHitBoxPresenter.cs    Collider-based heal receiver
└── Views/
    ├── HealthSystemView.cs           Composite health UI view
    └── HealthBar/
        ├── HealthbarSO.cs            Health bar visual configuration SO
        └── HealthbarView.cs          Animated health bar with DOTween
```

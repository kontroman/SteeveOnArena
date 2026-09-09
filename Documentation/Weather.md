# Погода

`WeatherManager` применяет направление, цвет и яркость солнца, skybox,
туман, ambient light и включение объекта дождя. Переход мгновенный.
`SelectLevelWindow` и `LevelDescription` вызывают
`ApplyLevelPreset(config.GetRandomWeatherPreset())` при выборе уровня.
Если `weatherPresets` пуст, используется одиночный `weatherPreset`.
Автоцикл менеджера выключен в обеих сценах.

## Наборы

Настройки находятся в `Assets/ScriptableObjects/Levels/Level*.asset`,
в списке Weather Presets инспектора LevelConfig. Каждый вариант имеет
одинаковую вероятность выбора; повторение при следующем входе возможно.

| Уровень | Пресеты |
| --- | --- |
| Village | Village_ClearDay, Village_Sunrise |
| Sand | Desert_Noon, Desert_DustHaze, Desert_Sunset, Desert_ColdNight |
| Mine | Mine_ColdDepths, Mine_DustyGloom, Mine_BlueMist |
| Forest | Forest_Sunny, Forest_MorningMist, Forest_CoolHaze, Forest_Moonlight |

Все 13 вариантов явно назначают существующий CubeMap skybox. Вечерние
варианты используют утреннее небо с тёплым направленным светом.
Пыль и дымка реализованы обычным туманом, без частиц.
Исходные одиночные пресеты сохранены как fallback.

## Ограничения текущего кода

- `rainIntensity` хранится и сравнивается, но не передаётся эффекту.
  В GameplayScene `rainParticles` не назначен; в SampleScene назначен.
  Во всех новых вариантах дождь выключен.
- `OnSceneLoaded` отписывается после первой загрузки сцены.
- `ToggleAutoCycle(true)` при повторных вызовах запускает дополнительные корутины.
- `skyboxMaterial == null` оставляет небо предыдущего пресета.
- Менеджер не задаёт режимы fog/ambient. В SampleScene и GameplayScene
  используются ExponentialSquared fog и Skybox ambient: ambient зависит
  от неба, поэтому `ambientLight` не является независимой настройкой заливки.

## Проверка в Unity

Запустить игру через SampleScene и войти на каждую карту. В Play Mode
у WeatherManager выбранный вариант добавляется в Presets; изменение его
полей применяется через Update. Проверить читаемость ресурсов и врагов
ночью и в тумане, а также смену неба после выхода из шахты на поверхность.
Визуальная проверка в Unity при добавлении наборов не выполнялась.

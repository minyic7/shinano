# Shinano Unity Project

A VRChat-ready anime character avatar with extensive customization options.

**Unity Version:** 2022.3.62f3

---

## 📁 Project Structure

```
Assets/
├── Shinano/          # Main character assets
├── Kirurobo/         # UniWindowController plugin
├── Scenes/           # Sample scenes
Packages/
└── jp.lilxyzw.liltoon/  # lilToon shader
```

---

## 🎭 Shinano Character Assets

### 3D Models
| File | Description |
|------|-------------|
| `FBX/Shinano.fbx` | Base character model |
| `FBX/Shinano_kisekae.fbx` | Character with costume toggle support |

### Prefabs
| File | Description |
|------|-------------|
| `Prefab/Shinano.prefab` | Ready-to-use character prefab |
| `Prefab/Shinano_kisekae.prefab` | Character with costume variations |

### Scenes
| File | Description |
|------|-------------|
| `Shinano/Shinano.unity` | Main character scene |
| `Scenes/SampleScene.unity` | Empty sample scene |

---

## 🎨 Materials

### Base Materials
- `Shinano_body.mat` / `Shinano_body_flat.mat` - Body
- `Shinano_face.mat` / `Shinano_face_alpha.mat` - Face
- `Shinano_costume.mat` - Costume
- `Shinano_hair.mat` - Hair

### Color Variants

**Costume (7 colors):**
autumn, black, kikyo, spring, summer, white, winter

**Hair (10 colors):**
autumn, black, brown, gold, kikyo, olive, spring, summer, white, winter

**Eyes (14 colors):**
autumn, black, blue, cyan, gray, green, kikyo, pink, red, spring, summer, white, winter, yellow

---

## 🖼️ Textures

### Base Textures
- `Shinano_body.png` / `Shinano_body_flat.png`
- `Shinano_costume.png`
- `Shinano_face.png`

### Maps
- `Map/Matcap.png` - Matcap lighting
- `Map/Shinano_costume_normal.png` - Costume normal map
- `Map/Shinano_hair_normal.png` - Hair normal map

### Makeup Variants
- `Make up/Shinano_face_makeup1-4.png`

### Masks
- Shadow, emission, alpha, outline, color masks

---

## 🎬 Animation

### Controllers
- `Shinano_FX.controller` - Effects/expressions controller
- `Shinano_Locomotion.controller` - Movement controller

### Gestures
- Finger point, Hand gun, Thumbs up

### VRChat Menu (EX Menu)
- `Sinano_main.asset` - Main menu
- `Sinano_main_kisekae.asset` - Costume menu
- Parameter assets for customization

---

## 🖱️ Icons

### Costume Icons
Boots, Costume, Dress, Skirt, Sweater, Tights

### Facial Expression Icons
Cheek_eye, Dead_eye, Guruguru_eye, Heart_eye, Kirakira_eye, White_eye, Sweat, Tear

### Hair Style Icons
Hair1 - Hair10

### Other Icons
Body, Breast, Hip, Ear, Tail, Sparkling

---

## 📦 Plugins

### UniWindowController
Desktop window management for transparent/borderless windows.
- Supports Windows (x86/x64) and macOS
- Location: `Assets/Kirurobo/UniWindowController/`

### lilToon Shader
Advanced toon shader for anime-style rendering.
- VRChat compatible
- Location: `Packages/jp.lilxyzw.liltoon/`

---

## 🚀 Quick Start

1. Open `Assets/Shinano/Shinano.unity`
2. Add a Camera if missing (GameObject → Camera)
3. Position camera at (0, 1, -2) to view character
4. Press Play to preview

---

## 📄 License

Please refer to individual asset licenses within the project.

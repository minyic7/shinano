"""
Blender Python Script to Simplify Shinano FBX for Mixamo
Run this script in Blender to create a Mixamo-compatible export

Usage:
1. Open Blender
2. Go to Scripting workspace
3. Open this file or paste the content
4. Click "Run Script"

Or run from command line:
blender --background --python simplify_for_mixamo.py
"""

import bpy
import os

# Configuration - Using Shinano.fbx (not kisekae) for better Mixamo compatibility
INPUT_FBX = "/Users/minyic/git/unity/shinano/Assets/Shinano/FBX/Shinano.fbx"
OUTPUT_FBX = "/Users/minyic/Desktop/Shinano_for_mixamo.fbx"

# Bones to KEEP (standard humanoid skeleton)
BONES_TO_KEEP = {
    # Core
    "Hips", "Spine", "Chest", "Neck", "Head",
    # Left Arm
    "Shoulder.L", "Upper_arm.L", "Lower_arm.L", "Hand.L",
    # Right Arm
    "Shoulder.R", "Upper_arm.R", "Lower_arm.R", "Hand.R",
    # Left Leg
    "Upper_leg.L", "Lower_leg.L", "Foot.L", "Toe.L",
    # Right Leg
    "Upper_leg.R", "Lower_leg.R", "Foot.R", "Toe.R",
    # Left Hand Fingers
    "Thumb Proximal.L", "Thumb Intermediate.L", "Thumb Distal.L",
    "Index Proximal.L", "Index Intermediate.L", "Index Distal.L",
    "Middle Proximal.L", "Middle Intermediate.L", "Middle Distal.L",
    "Ring Proximal.L", "Ring Intermediate.L", "Ring Distal.L",
    "Little Proximal.L", "Little Intermediate.L", "Little Distal.L",
    # Right Hand Fingers
    "Thumb Proximal.R", "Thumb Intermediate.R", "Thumb Distal.R",
    "Index Proximal.R", "Index Intermediate.R", "Index Distal.R",
    "Middle Proximal.R", "Middle Intermediate.R", "Middle Distal.R",
    "Ring Proximal.R", "Ring Intermediate.R", "Ring Distal.R",
    "Little Proximal.R", "Little Intermediate.R", "Little Distal.R",
    # Eyes (optional, Mixamo might use these)
    "LeftEye", "RightEye",
}

# Meshes to KEEP - Body and Head for better Mixamo auto-rigging
MESHES_TO_KEEP = {"Body_base", "Body", "Head", "Face"}

def clear_scene():
    """Clear the current scene"""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()

def import_fbx(filepath):
    """Import FBX file"""
    print(f"Importing: {filepath}")
    bpy.ops.import_scene.fbx(filepath=filepath)
    print("Import complete!")

def get_armature():
    """Find the armature object"""
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE':
            return obj
    return None

def delete_extra_meshes():
    """Delete all meshes except Body_base"""
    print("Deleting extra meshes...")
    meshes_to_delete = []
    
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            if obj.name not in MESHES_TO_KEEP and not any(keep in obj.name for keep in MESHES_TO_KEEP):
                meshes_to_delete.append(obj)
                print(f"  - Marking for deletion: {obj.name}")
    
    # Delete the meshes
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes_to_delete:
        obj.select_set(True)
    bpy.ops.object.delete()
    
    print(f"Deleted {len(meshes_to_delete)} meshes")

def delete_extra_bones():
    """Delete all bones except the humanoid skeleton"""
    print("Deleting extra bones...")
    
    armature = get_armature()
    if not armature:
        print("ERROR: No armature found!")
        return
    
    # Select armature and enter edit mode
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode='EDIT')
    
    # Collect bones to delete
    bones_to_delete = []
    for bone in armature.data.edit_bones:
        if bone.name not in BONES_TO_KEEP:
            bones_to_delete.append(bone.name)
            print(f"  - Marking for deletion: {bone.name}")
    
    # Delete bones
    for bone_name in bones_to_delete:
        bone = armature.data.edit_bones.get(bone_name)
        if bone:
            armature.data.edit_bones.remove(bone)
    
    bpy.ops.object.mode_set(mode='OBJECT')
    print(f"Deleted {len(bones_to_delete)} bones")

def cleanup_orphan_data():
    """Remove orphaned mesh and armature data"""
    # Remove unused meshes
    for mesh in bpy.data.meshes:
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    
    # Remove unused armatures
    for arm in bpy.data.armatures:
        if arm.users == 0:
            bpy.data.armatures.remove(arm)

def delete_armature():
    """Delete the armature so Mixamo can create its own rig"""
    print("Deleting armature for clean Mixamo rigging...")
    
    # Find and delete armatures
    armatures_to_delete = []
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE':
            armatures_to_delete.append(obj)
            print(f"  - Marking for deletion: {obj.name}")
    
    # Clear parent but keep transforms for meshes
    for obj in bpy.data.objects:
        if obj.type == 'MESH' and obj.parent:
            if obj.parent.type == 'ARMATURE':
                # Clear parent but keep position
                matrix = obj.matrix_world.copy()
                obj.parent = None
                obj.matrix_world = matrix
    
    # Delete armatures
    bpy.ops.object.select_all(action='DESELECT')
    for obj in armatures_to_delete:
        obj.select_set(True)
    bpy.ops.object.delete()
    
    print(f"Deleted {len(armatures_to_delete)} armatures")

def export_fbx(filepath):
    """Export to FBX for Mixamo - mesh only, no skeleton"""
    print(f"Exporting to: {filepath}")
    
    # Select only mesh objects
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            obj.select_set(True)
    
    # Export settings optimized for Mixamo (mesh only, no armature)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_NONE',
        object_types={'MESH'},  # Only export meshes
        use_mesh_modifiers=True,
        mesh_smooth_type='OFF',
        bake_anim=False,
        path_mode='AUTO',
        embed_textures=False,
        axis_forward='-Z',
        axis_up='Y',
    )
    print("Export complete!")

def main():
    """Main function"""
    print("=" * 50)
    print("Shinano FBX Simplifier for Mixamo")
    print("(Exporting MESH ONLY - no skeleton)")
    print("=" * 50)
    
    # Check if input file exists
    if not os.path.exists(INPUT_FBX):
        print(f"ERROR: Input file not found: {INPUT_FBX}")
        return
    
    # Clear scene
    clear_scene()
    
    # Import FBX
    import_fbx(INPUT_FBX)
    
    # Delete extra meshes (keep only Body_base and head)
    delete_extra_meshes()
    
    # Delete the armature entirely - Mixamo will create its own
    delete_armature()
    
    # Cleanup orphan data
    cleanup_orphan_data()
    
    # Export simplified FBX (mesh only)
    export_fbx(OUTPUT_FBX)
    
    print("=" * 50)
    print("DONE!")
    print(f"Output saved to: {OUTPUT_FBX}")
    print("")
    print("Next steps:")
    print("1. Go to mixamo.com")
    print("2. Upload: Shinano_for_mixamo.fbx")
    print("3. Mixamo will AUTO-RIG the mesh (place markers if asked)")
    print("4. Download animations with 'Without Skin' option")
    print("=" * 50)

# Run the script
if __name__ == "__main__":
    main()

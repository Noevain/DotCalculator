using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui.NamePlate;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace DotCalculator;

public unsafe class NameplateHandler : IDisposable
{
    Plugin _plugin;
    private AddonNamePlate* currNameplateAddon = null;
    private static AtkTextNode*[] mDotTextNodes = new AtkTextNode*[AddonNamePlate.NumNamePlateObjects];
    //	Note: Node IDs only need to be unique within a given addon.
    internal const uint mNameplateDistanceNodeIDBase = 0x7668C400;  //YOLO hoping for no collisions.
    public NameplateHandler(Plugin plugin)
    {
        _plugin = plugin;
        //Service.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "NamePlate",PreDrawHandler);
    }
    public void Dispose()
    {
        //DestroyDotNodes();
        //Service.AddonLifecycle.UnregisterListener(AddonEvent.PostDraw, "NamePlate",PreDrawHandler);
    }

    public unsafe void PreDrawHandler(AddonEvent type, AddonArgs args)
    {
        //CS 7.3 breaking changes,will fix later
       /* var pNameplateAddon = (AddonNamePlate*)args.Addon;

        try
        {
            if (currNameplateAddon != pNameplateAddon)
            {
                //initial build
                //the addon seems to take care of cleaning up our nodes on reload,just clear our the stale references
                for (int i = 0; i < mDotTextNodes.Length; i++)
                {
                    mDotTextNodes[i] = null;
                }

                currNameplateAddon = pNameplateAddon;
                if (currNameplateAddon != null) CreateDotNodes();
            }
            UpdateDotNodes();
        }
        catch (Exception e) {Service.Log.Error(e.ToString()); }
        
        */

    }

    private void CreateDotNodes()
    {
        for( int i = 0; i < AddonNamePlate.NumNamePlateObjects; ++i )
        {
            var nameplateObject = GetNameplateObject( i );
            if( nameplateObject == null )
            {
                Service.Log.Warning( $"Unable to obtain nameplate object for index {i}" );
                continue;
            }
            var pNameplateResNode = nameplateObject.Value.NameContainer;

            //	Make a node.
            var pNewNode = AtkNodeHelpers.CreateOrphanTextNode( mNameplateDistanceNodeIDBase + (uint)i, TextFlags.Edge | TextFlags.Glare );

            //	Set up the node in the addon.
            if( pNewNode != null )
            {
                var pLastChild = pNameplateResNode->ChildNode;
                while( pLastChild->PrevSiblingNode != null ) pLastChild = pLastChild->PrevSiblingNode;
                pNewNode->AtkResNode.NextSiblingNode = pLastChild;
                pNewNode->AtkResNode.ParentNode = pNameplateResNode;
                pLastChild->PrevSiblingNode = (AtkResNode*)pNewNode;
                nameplateObject.Value.RootComponentNode->Component->UldManager.UpdateDrawNodeList();
                pNewNode->AtkResNode.SetUseDepthBasedPriority( false );
                //	Store it in our array.
                mDotTextNodes[i] = pNewNode;

                Service.Log.Verbose( $"Attached new text node for nameplate {i} (0x{(IntPtr)pNewNode:X})." );
            }
            else
            {
                Service.Log.Warning( $"Unable to create new text node for nameplate {i}." );
            }
        }
    }

    private void UpdateDotNodes()
    {
        if (_plugin.InPvp) return;
        
        
        for (int i = 0; i < mDotTextNodes.Length; ++i)
        {
            if (mDotTextNodes[i] != null)
            {
                var nameplate = GetNameplateObject(i);
                
                if (nameplate == null) continue;
                var kind = nameplate.Value.NamePlateKind;
                var asTxt = mDotTextNodes[i]->GetAsAtkTextNode();
                if (kind != UIObjectKind.BattleNpcEnemy || nameplate.Value.IsLocalPlayer || nameplate.Value.IsPlayerCharacter)
                {
                    asTxt->ToggleVisibility(false);
                    continue;
                }
                //unfortunately NameplateObject does not contain the object nameplated, so we have to look it up
                //null check everything because everything here can dissapear, for instance when the entity dies
                if (Framework.Instance() != null && 
                    Framework.Instance()->GetUIModule()->GetUI3DModule() != null)
                {
                    var ui3Dmodule = Framework.Instance()->GetUIModule()->GetUI3DModule();
                    for (int j = 0; j < ui3Dmodule->NamePlateObjectInfoCount; j++)
                    {
                        var pObjectInfo = ui3Dmodule->NamePlateObjectInfoPointers[j].Value;
                        if (pObjectInfo != null &&
                            pObjectInfo->GameObject != null &&
                            pObjectInfo->NamePlateIndex < mDotTextNodes.Length)
                        {
                            var objId = pObjectInfo->GameObject->EntityId;
                            if (_plugin.calculator.IDtoRunningDamage.TryGetValue(objId, out int damage))
                            {
                                asTxt->SetText(damage.ToString());
                                asTxt->TextColor.A = 255;
                                asTxt->TextColor.R = 255;
                                asTxt->TextColor.G = 255;
                                asTxt->TextColor.B = 255;
                                asTxt->ToggleVisibility(true);
                            }
                            else
                            {
                                asTxt->ToggleVisibility(false);
                            }
                            
                        }

                    }
                }
                
            }
        }

    }

    private void DestroyDotNodes()
    {
    //Disabled because of CS breaking changes, will fix later
        //	If the addon has moved since disabling the hook, it's impossible to know whether our
        //	node pointers are valid anymore, so we have to just let them leak in that case.
        /*var pCurrentNameplateAddon = (AddonNamePlate*)Service.GameGui.GetAddonByName("NamePlate", 1);
        if (currNameplateAddon == null || pCurrentNameplateAddon != currNameplateAddon)
        {
            Service.Log.Warning($"Unable to cleanup nameplate nodes due to addon address mismatch during unload");
        }

        for (int i = 0; i < AddonNamePlate.NumNamePlateObjects; ++i)
        {
            var pTextNode = mDotTextNodes[i];
            var pNameplateNode = GetNameplateComponentNode(i);
            if (pTextNode != null && pNameplateNode != null)
            {
                try
                {
                    if (pTextNode->AtkResNode.PrevSiblingNode != null)
                        pTextNode->AtkResNode.PrevSiblingNode->NextSiblingNode = pTextNode->AtkResNode.NextSiblingNode;
                    if (pTextNode->AtkResNode.NextSiblingNode != null)
                        pTextNode->AtkResNode.NextSiblingNode->PrevSiblingNode = pTextNode->AtkResNode.PrevSiblingNode;
                    pNameplateNode->Component->UldManager.UpdateDrawNodeList();
                    pTextNode->AtkResNode.Destroy(true);
                    mDotTextNodes[i] = null;
                    Service.Log.Verbose($"Cleanup of nameplate {i} complete.");
                }
                catch (Exception e)
                {
                    Service.Log.Error(
                        $"Unknown error while removing text node 0x{(IntPtr)pTextNode:X} for nameplate {i} on component node 0x{(IntPtr)pNameplateNode:X}:\r\n{e}");
                }
            }

        }
        */

    }

    private AddonNamePlate.NamePlateObject? GetNameplateObject( int i )
    {
        if( i < AddonNamePlate.NumNamePlateObjects &&
            currNameplateAddon != null &&
            currNameplateAddon->NamePlateObjectArray[i].RootComponentNode != null )
        {
            return currNameplateAddon->NamePlateObjectArray[i];
        }
        else
        {
            return null;
        }
    }
    private AtkComponentNode* GetNameplateComponentNode( int i )
    {
        var nameplateObject = GetNameplateObject( i );
        return nameplateObject != null ? nameplateObject.Value.RootComponentNode : null;
    }
    
    
    
}

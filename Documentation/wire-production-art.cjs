const fs=require('fs'),path=require('path'),crypto=require('crypto');
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n');
const write=(p,s)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,s.replace(/_animationIDEL: \{[^\n]+\}/g,'_animationIDEL: {fileID: 6435626339322127653}'));};
const guid=p=>read(p+'.meta').match(/guid: (\w+)/)[1];
const ref=(p,f=11400000,t=2)=>`{fileID: ${f}, guid: ${guid(p)}, type: ${t}}`;
const hash=p=>crypto.createHash('md5').update('MineArena-production-v1:'+p).digest('hex');
function meta(p,kind='NativeFormatImporter',fileId=11400000){if(!fs.existsSync(p+'.meta'))write(p+'.meta',`fileFormatVersion: 2\nguid: ${hash(p)}\n${kind}:\n  externalObjects: {}\n  mainObjectFileID: ${fileId}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);}
function field(s,k,v){const re=new RegExp('^  '+k+':[^\\n]*(?:\\n(?!  [A-Za-z_])[ \\t]+[^\\n]*)*','m');return re.test(s)?s.replace(re,'  '+k+': '+v):s+'  '+k+': '+v+'\n';}
const folder='Assets/Art/Production/';
const manifest=JSON.parse(read('Documentation/production-art-prompts.json'));
for(const entry of manifest){
 const p=folder+entry.id+'.png';fs.mkdirSync(folder,{recursive:true});
 if(fs.existsSync(entry.source)) fs.copyFileSync(entry.source,p);
 else if(!fs.existsSync(p)) throw new Error('Missing generated art: '+p);
 if(!fs.existsSync(p+'.meta')){
  let m=read('Assets/Textures/Icons/Items/iron_ingot.png.meta').replace(/^guid: \w+/m,'guid: '+hash(p));
  m=m.replace(/maxTextureSize: \d+/g,'maxTextureSize: 128').replace(/textureCompression: \d+/g,'textureCompression: 0');
  if(entry.id==='wool-tile')m=m.replace(/wrapU: \d/,'wrapU: 0').replace(/wrapV: \d/,'wrapV: 0');
  write(p+'.meta',m);
 }
}
const item=id=>'Assets/ScriptableObjects/Configs/Drops/'+id+'.asset';
const icons={String:'string',Leather:'leather',Stick:'stick',NetheriteScrap:'netherite-scrap',Wool:'wool-tile',Wheat:'wheat',SugarCane:'sugar-cane',NetherWart:'nether-wart',Sugar:'sugar',GlassBottle:'glass-bottle',MelonSlice:'melon-slice',GlisteringMelon:'glistering-melon',GhastTear:'ghast-tear',HealingPotion:'potion-healing',RegenerationPotion:'potion-regeneration',SpeedPotion:'potion-speed'};
const existing={IronIngot:'Assets/Textures/Icons/Items/iron_ingot.png',GoldIngot:'Assets/Textures/Icons/Items/gold_ingot.png',NetheriteIngot:'Assets/Textures/Icons/Items/netherite_ingot.png',Glass:'Assets/Textures/Blocks/Glass/glass.png'};
const textures={...Object.fromEntries(Object.entries(icons).map(([id,name])=>[id,folder+name+'.png'])),...existing};
function ensureSprite(p){let m=read(p+'.meta');m=m.replace(/textureType: \d+/,'textureType: 8').replace(/spriteMode: \d+/,'spriteMode: 1');write(p+'.meta',m);}
for(const p of Object.values(textures))ensureSprite(p);
function material(id,texture){
 const p=folder+'Materials/'+id+'.mat';
 write(p,`%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: ${id}\n  m_Shader: ${ref('Assets/Scripts/Item/PixelItem.shader',4800000,3)}\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 1\n  m_CustomRenderQueue: -1\n  stringTagMap: {}\n  disabledShaderPasses: []\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs:\n    - _MainTex:\n        m_Texture: ${ref(texture,2800000,3)}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n    m_Ints: []\n    m_Floats: []\n    m_Colors:\n    - _Color: {r: 1, g: 1, b: 1, a: 1}\n`);meta(p,'NativeFormatImporter',2100000);return p;
}
const rootPickup=1294500603521610;const mats={};
for(const [id,texture] of Object.entries(textures)){
 if(!fs.existsSync(texture))throw Error('Missing generated art '+texture);
 const cube=['Wool','Glass'].includes(id),mat=material(id,texture);mats[id]=mat;
 let s=read(item(id));s=field(s,'_icon',ref(texture,21300000,3));s=field(s,'_blockStyleIcon',cube?1:0);
 const p='Assets/Prefabs/Objects/ProductionDrops/'+id+'Drop.prefab';
 let prefab=read('Assets/Prefabs/Objects/OresDrop/WoodDrop.prefab');
 prefab=prefab.replace('m_Name: WoodDrop','m_Name: '+id+'Drop').replace(/  _item: .*/,'  _item: '+ref(item(id)));
 // Only change the pickup body; its glow and landing behaviour are retained.
 prefab=prefab.replace(/(--- !u!23 &5315548543111247811[\s\S]*?m_Materials:\n  - )[^\n]+/,'$1'+ref(mat,2100000,2));
 if(!cube)prefab=prefab.replace(/m_Mesh: \{fileID: 10202, /,'m_Mesh: {fileID: 10210, ');
 write(p,prefab);meta(p,'PrefabImporter');s=field(s,'_prefab',ref(p,rootPickup,3));write(item(id),s);
}
// Use the existing three-face UI prefab with genuinely distinct log/debris faces.
for(const [id,top,side] of [
 ['WoodBirch','Wood/birch_log_top.png','Wood/birch_log.png'],
 ['WoodDark','Wood/dark_oak_log_top.png','Wood/dark_oak_log.png'],
 ['WoodOak','Wood/jungle_log_top.png','Wood/oak_log.png'],
 ['NetheriteOre','Stone/ancient_debris_top.png','Stone/ancient_debris_side.png']]){
 ensureSprite('Assets/Textures/Blocks/'+top);ensureSprite('Assets/Textures/Blocks/'+side);
 let s=read(item(id));s=field(s,'_topIcon',ref('Assets/Textures/Blocks/'+top,21300000,3));s=field(s,'_sideIcon',ref('Assets/Textures/Blocks/'+side,21300000,3));s=field(s,'_blockStyleIcon',1);write(item(id),s);
}
for(const id of ['Stone','Sand','CoalOre','IronOre','GoldOre','DiamondOre','Planks'])write(item(id),field(read(item(id)),'_blockStyleIcon',1));
const resourceRoot=8637239530525059325;
const nodes={Wheat:{plant:'Wheat',min:2,max:4},SugarCane:{plant:'SugarCane',min:2,max:3},NetherWart:{plant:'NetherWart',min:1,max:2},Wool:{min:1,max:2},Glass:{min:1,max:2}};
for(const [id,options] of Object.entries(nodes)){
 let s=read('Assets/Prefabs/Objects/Ores/Stone.prefab').replace('m_Name: Stone','m_Name: '+id+'Harvest');
 s=s.replace(/(m_Materials:\n  - )[^\n]+/,'$1'+ref(mats[id],2100000,2));
 s=s.replace(/  _dropTable:[\s\S]*?(?=  _isOneDrop:)/,'  _dropTable:\n  - Item: '+ref(item(id))+'\n    DropChance: 100\n    MinQuantity: '+options.min+'\n    MaxQuantity: '+options.max+'\n');
 if(options.plant){
  const cid=777700000000001;
  s=s.replace('  m_Layer: 0','  - component: {fileID: '+cid+'}\n  m_Layer: 0');
  s+=`--- !u!114 &${cid}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${resourceRoot}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: ${ref('Assets/Scripts/Item/HarvestPlantVisual.cs',11500000,3)}\n  m_Name: \n  m_EditorClassIdentifier: \n  _plantMaterial: ${ref('Assets/Textures/Materials/Nature/'+options.plant+'.mat',2100000,2)}\n`;
 }
 const p='Assets/Prefabs/Objects/Harvest/'+id+'Harvest.prefab';write(p,s);meta(p,'PrefabImporter');
}
for(const [level,ids] of [['Level1_Village',['Wheat','Wool']],['Level2_Sand',['SugarCane','Glass']],['Level3_Mine',['NetherWart']],['Level4_Forest',['Wheat','SugarCane','NetherWart']]]){
 const p='Assets/ScriptableObjects/Levels/'+level+'.asset';let s=read(p);
 for(const id of ids){
  const node='Assets/Prefabs/Objects/Harvest/'+id+'Harvest.prefab';
  if(!s.includes(guid(node)))s=s.replace('  rewardResources:','  - resource: '+ref(node,resourceRoot,3)+'\n    spawnChance: 0.15\n  rewardResources:');
  const section=s.match(/  availableResources:[\s\S]*?(?=  resourceSpawnConfigs:)/)[0];
  if(!section.includes(guid(item(id))))s=s.replace(section,section+'  - '+ref(item(id))+'\n');
 }
 write(p,s);
}
console.log('Art, materials, pickups and five harvestable prefabs wired.');

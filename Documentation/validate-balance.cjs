const fs=require('fs'), assert=require('assert');
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n');
const walk=p=>fs.readdirSync(p,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(p+'/'+e.name):[p+'/'+e.name]);
const files=walk('Assets');
const byGuid=new Map();
for(const p of files.filter(p=>p.endsWith('.meta'))) { const m=read(p).match(/^guid: (\w+)/m); if(m) byGuid.set(m[1],p.slice(0,-5)); }
const assetFiles=files.filter(p=>p.startsWith('Assets/ScriptableObjects/')&&p.endsWith('.asset'));
const paths=Object.fromEntries(assetFiles.map(p=>[p.split('/').pop().replace('.asset',''),p]));
paths.DiamondChestplate=paths.DiamondArmor;
const guid=p=>read(p+'.meta').match(/^guid: (\w+)/m)[1];
const config=JSON.parse(read('Documentation/balance-data.json'));
const logs=[];
const check=(ok,msg)=>{assert(ok,msg);logs.push('PASS '+msg);};
for(const id of ['Fiber','Resin','Rope','Fabric','SteelIngot','ReinforcedPlanks'])
 check(!paths[id]&&!JSON.stringify(config).includes('"'+id+'"'),id+' removed from assets and balance');
check(JSON.stringify(config.recipes.BowItem.cost)===JSON.stringify({Stick:3,String:3}),'Bow costs three sticks and three strings');
check(JSON.stringify(config.recipes.NetheriteIngot.cost)===JSON.stringify({NetheriteScrap:4,GoldIngot:4}),'Netherite ingot costs four scraps and four gold ingots');
for(const id of ['String','Leather']) check(read(paths[id]).includes('  _craftCosts: []'),id+' has no fabricated crafting recipe');
for(const [mob,item] of [['Spider','String'],['Wolf','Leather'],['Bear','Leather']]) {
 const text=read('Assets/Prefabs/Mobs/'+mob+'.prefab');
 check(text.includes(guid(paths[item])),mob+' drops '+item);
 check((text.match(/  _dropOnDeath: 1/g)||[]).length===1,mob+' death loot enabled once');
}
const itemDb=read(paths.ItemDatabase), buildingDb=read(paths.BuildingsDatabase);
const registered=[...itemDb.slice(itemDb.indexOf('  allItems:')).matchAll(/guid: (\w+)/g)].map(m=>byGuid.get(m[1]));
check(registered.every(Boolean),'Every inventory database reference resolves');
const ids=registered.map(p=>read(p).match(/^  _name: (.+)$/m)[1]);
check(new Set(ids).size===ids.length,'Inventory IDs are unique');
const poolText=read('Assets/Scripts/ObjectPool/Pools/MobsPoolsPeresets.asset');
const poolPrefabs=[...poolText.slice(poolText.indexOf('  Preset:')).matchAll(/guid: (\w+)/g)].map(m=>byGuid.get(m[1]));
for(const mob of Object.keys(config.mobs)) {
 const scriptGuid=guid('Assets/Scripts/AI/ConcreteMobs/'+mob+'.cs');
 const prefab=poolPrefabs.find(p=>p&&read(p).includes(scriptGuid));
 check(!!prefab,mob+' has a registered spawn pool');
 check(read(prefab).includes(guid('Assets/Scripts/AI/MobPresets/'+mob+'.asset')),mob+' uses its balanced preset');
}
const itemName=id=>read(paths[id]).match(/^  _name: (.+)$/m)[1];
for(const [id,recipe] of Object.entries(config.recipes)) {
 const s=read(paths[id]);
 check(itemDb.includes(guid(paths[id])),id+' registered');
 for(const [cost,n] of Object.entries(recipe.cost)) {
  check(n>0&&itemDb.includes(guid(paths[cost])),id+' cost '+cost+' valid');
  check(s.includes(guid(paths[cost])),id+' cost serialized '+cost);
 }
 check(s.includes('  _craftAmount: '+recipe.output),id+' output serialized');
}
const unlocks=new Map();
for(const [id,levels] of Object.entries(config.buildings)) {
 const s=read(paths[id]);
 check(buildingDb.includes(guid(paths[id])),id+' registered');
 check((s.match(/  - _level: /g)||[]).length===levels.length,id+' has expected levels');
 const names=[...s.matchAll(/    _modelPrefab: .*guid: (\w+)/g)];
 check(names.length===levels.length&&names.every(m=>byGuid.has(m[1])),id+' models resolve');
 for(const [i,[cost,items]] of levels.entries()) for(const item of items) {
  check(!unlocks.has(item),item+' has one owning building');
  unlocks.set(item,[id,i+1]);
 }
}
// Fixed point on real recipe/building dependencies: no self-locked upgrades.
const available=new Set(config.encounters.flatMap(e=>Object.keys(e[3])));
const built=Object.fromEntries(Object.keys(config.buildings).map(id=>[id,0]));
for(let pass=0;pass<100;pass++) {
 let changed=false;
 for(const [id,levels] of Object.entries(config.buildings)) {
  const next=levels[built[id]];
  if(next&&Object.keys(next[0]).every(x=>available.has(x))) {built[id]++;changed=true;}
 }
 for(const outputs of (config.production||[]).slice(0,built.FarmBuilding||0))
  for(const id of Object.keys(outputs)) if(!available.has(id)) {available.add(id);changed=true;}
 for(const [id,{cost}] of Object.entries(config.recipes)) {
  const gate=unlocks.get(id);
  if(!available.has(id)&&(!gate||built[gate[0]]>=gate[1])&&Object.keys(cost).every(x=>available.has(x))) {available.add(id);changed=true;}
 }
 if(!changed) break;
}
check(Object.keys(config.recipes).every(id=>available.has(id)),'All '+Object.keys(config.recipes).length+' recipes reachable from expedition resources');
check(Object.entries(built).every(([id,n])=>n===config.buildings[id].length),'Every building reaches maximum level without dependency cycles');
for(const [id,counts,types] of config.encounters) {
 const s=read(paths[id]);
 check(!s.includes('  portalPrefab: {fileID: 0}'),id+' exit portal assigned');
 check(counts.reduce((a,b)=>a+b,0)===[12,16,20,24][config.encounters.findIndex(e=>e[0]===id)],id+' encounter size');
 const packed=[...s.matchAll(/    MobTypes: ([a-f0-9]+)/g)].map(m=>m[1]);
 check(packed.length===3&&packed.every((v,i)=>v.length===types[i].length*8),id+' enemy lists serialized in Unity format');
 for(const m of s.matchAll(/guid: (\w+)/g)) check(byGuid.has(m[1]),id+' reference '+m[1]+' resolves');
}
for(const p of files.filter(p=>p.startsWith('Assets/Prefabs/Objects/Ores/')&&p.endsWith('.prefab'))) {
 const s=read(p), match=s.match(/  _dropTable:([\s\S]*?)(?=  _isOneDrop:|---|$)/);
 if(!match) continue;
 check(!match[1].includes('Item: {fileID: 0}'),p+' has no empty drops');
 for(const m of match[1].matchAll(/Item: .*guid: (\w+)/g)) {
  const item=byGuid.get(m[1]);
  check(!!item,p+' drop resolves');
  const prefab=read(item).match(/  _prefab: .*guid: (\w+)/);
  if(prefab) {
   const pickup=byGuid.get(prefab[1]);
   check(!!pickup&&read(pickup).includes(m[1]),p+' pickup points back to its resource');
  }
 }
}
for(const p of files.filter(p=>/^Assets\/(Art\/Production|Prefabs\/Objects\/(Harvest|ProductionDrops))\//.test(p)&&/\.(mat|prefab)$/.test(p))) {
 for(const m of read(p).matchAll(/guid: (\w+)/g)) check(byGuid.has(m[1])||/^0{16}[ef]0{15}$/.test(m[1]),p+' reference resolves '+m[1]);
}
for(const id of ['String','Leather','HealingPotion','RegenerationPotion','SpeedPotion','Wheat','SugarCane','NetherWart']) {
 const s=read(paths[id]);
 check(s.includes('_blockStyleIcon: 0'),id+' uses a flat icon');
 const drop=byGuid.get(s.match(/_prefab: .*guid: (\w+)/)[1]);
 check(read(drop).includes(guid(paths[id])),id+' pickup backlink correct');
}
for(const id of ['Wool','Glass','WoodOak','WoodBirch','WoodDark','NetheriteOre'])
 check(read(paths[id]).includes('_blockStyleIcon: 1'),id+' uses three-face icon');
fs.writeFileSync('Documentation/balance-validation.txt',logs.join('\n')+'\n');
console.log(logs.length+' checks passed.');

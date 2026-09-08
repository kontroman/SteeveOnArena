// Read-only balance model. Run from project root: node Documentation/simulate-playtime.cjs
// Reads current Unity assets, not balance-data.json. Does not simulate navigation or player skill.
const fs = require('fs');
const read = p => fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, '').replace(/\r/g, '');
const walk = p => fs.readdirSync(p, {withFileTypes:true}).flatMap(e => e.isDirectory() ? walk(p+'/'+e.name) : [p+'/'+e.name]);
const files = walk('Assets');
const guidPaths = new Map(files.filter(p=>p.endsWith('.meta')).map(p=>[read(p).match(/^guid: (\w+)/m)?.[1],p.slice(0,-5)]));
const scalar = (s,k,d=0) => Number(s.match(new RegExp('^ *'+k+': ([^\\n]+)','m'))?.[1] ?? d);
const section = (s,key,indent=2) => s.match(new RegExp('^ {'+indent+'}'+key+':[^\\n]*\\n([\\s\\S]*?)(?=^ {'+indent+'}[A-Za-z_]|$(?![\\s\\S]))','m'))?.[1] || '';
const refs = s => [...s.matchAll(/guid: (\w+)/g)].map(m=>guidPaths.get(m[1]));
const idFor = p => read(p).match(/^  _name: (.+)$/m)?.[1] || p.split('/').pop().replace('.asset','');
const costs = s => Object.fromEntries([...s.matchAll(/_resource: .*guid: (\w+).*\n +_amount: (\d+)/g)].map(m=>[idFor(guidPaths.get(m[1])),Number(m[2])]));
const items = {};
for(const p of files.filter(p=>p.startsWith('Assets/ScriptableObjects/Configs/')&&p.endsWith('.asset'))){
 const s=read(p); if(!/^  _name: /m.test(s))continue;
 const id=idFor(p); items[id]={path:p,cost:costs(s),output:scalar(s,'_craftAmount',1),seconds:scalar(s,'_craftSeconds',7),stackable:scalar(s,'_stackable')===1,resist:scalar(s,'_resist'),category:s.match(/^  _resourceCategory: (.+)$/m)?.[1]||id};
}
const buildings={},owners={};
for(const p of files.filter(p=>p.startsWith('Assets/ScriptableObjects/Configs/Buildings/')&&p.endsWith('.asset'))){
 const name=p.split('/').pop().replace('.asset','');
 buildings[name]=read(p).split(/^  - _level: /m).slice(1).map(s=>{
  const level=parseInt(s),unlocks=refs(section(s,'_unlocks',4)).map(idFor);
  unlocks.forEach(id=>owners[id]=[name,level]);
  return {level,cost:costs(s),unlocks,bonus:scalar(s,'_expeditionRewardBonusPercent')};
 });
}
const mobNames=['Zombie','Skeleton','Pillager','Wolf','Bear','Enderman','Witch','Spider','Creeper','ArcherPillager'];
const mobs=Object.fromEntries(mobNames.map(name=>{const s=read('Assets/Scripts/AI/MobPresets/'+name+'.asset');return [name,{hp:scalar(s,'MaxHealth'),damage:scalar(s,'Damage'),delay:scalar(s,'AttackDelay'),speed:scalar(s,'Speed')}]}));
const weapons=Object.fromEntries(files.filter(p=>p.startsWith('Assets/ScriptableObjects/Configs/Equipment/Swords/')&&p.endsWith('.asset')&&read(p).includes('  BaseDamage:')).map(p=>{const s=read(p);return [p.split('/').pop().replace('.asset',''),{damage:scalar(s,'BaseDamage'),cooldown:scalar(s,'Cooldown')}]}));
const oreGuid=read('Assets/Scripts/Levels/OreSpawnPoint.cs.meta').match(/^guid: (\w+)/m)[1];
function drops(p){
 const s=read(p),result={}; let remaining=1;
 for(const m of s.matchAll(/Item: .*guid: (\w+).*\n +DropChance: ([\d.]+)\n +MinQuantity: (\d+)\n +MaxQuantity: (\d+)/g)){
  const id=idFor(guidPaths.get(m[1])), chance=Number(m[2])/100;
  result[id]=(result[id]||0)+remaining*chance*(Number(m[3])+Number(m[4]))/2;
  if(s.includes('_isOneDrop: 1'))remaining*=1-chance;
 } return result;
}
const mobDrops=Object.fromEntries(mobNames.map(name=>{
 const scriptGuid=read('Assets/Scripts/AI/ConcreteMobs/'+name+'.cs.meta').match(/^guid: (\w+)/m)[1];
 const prefab=files.find(p=>p.startsWith('Assets/Prefabs/Mobs/')&&p.endsWith('.prefab')&&read(p).includes(scriptGuid));
 return [name,prefab&&read(prefab).includes('_dropOnDeath: 1')?drops(prefab):{}];
}));
const levels=files.filter(p=>/Assets\/ScriptableObjects\/Levels\/Level\d_.*\.asset$/.test(p)).sort().map(p=>{
 const s=read(p),rewards={};
 for(const m of section(s,'rewardResources').matchAll(/Item: .*guid: (\w+).*\n +Amount: (\d+)/g))rewards[idFor(guidPaths.get(m[1]))]=Number(m[2]);
 const arenaPath=guidPaths.get(s.match(/  levelPrefab: .*guid: (\w+)/)[1]);
 const points=(read(arenaPath).match(new RegExp('guid: '+oreGuid,'g'))||[]).length;
 const spawnMatches=[...section(s,'resourceSpawnConfigs').matchAll(/resource: .*guid: (\w+).*\n +spawnChance: ([\d.]+)/g)];
 const weightTotal=spawnMatches.reduce((a,m)=>a+Number(m[2]),0);
 const spawns=spawnMatches.map(m=>{
  const configured=Number(m[2]), probability=configured/weightTotal;
  const path=guidPaths.get(m[1]);return {path,configured,probability,drops:drops(path)};
 });
 const waves=s.split(/^  - MobCount: /m).slice(1).map(t=>{const hex=t.match(/MobTypes: (\w+)/)[1];return {count:parseInt(t),delay:scalar(t,'DelayBetweenMobs'),types:Array.from({length:hex.length/8},(_,i)=>mobNames[Buffer.from(hex.slice(i*8,i*8+8),'hex').readUInt32LE()])};});
 const total=waves.reduce((a,w)=>a+w.count,0),required=Math.ceil(total*scalar(s,'requiredKillPercentToOpenPortal'));
 let t=4,n=0,earliest=0; for(const w of waves){for(let i=0;i<w.count;i++){if(i)t+=w.delay;n++;if(n===required)earliest=t;}t+=12;}
 const hp=waves.reduce((sum,w)=>sum+w.count*w.types.reduce((a,name)=>a+mobs[name].hp,0)/w.types.length,0);
 const mining={}; for(const spawn of spawns)for(const [id,amount]of Object.entries(spawn.drops))mining[id]=(mining[id]||0)+points*spawn.probability*amount;
 const hits=Object.fromEntries(Object.entries(weapons).map(([id,weapon])=>[id,waves.reduce((sum,w)=>sum+w.count*w.types.reduce((a,name)=>a+Math.ceil(mobs[name].hp/weapon.damage),0)/w.types.length,0)]));
 const enemyDrops={};for(const w of waves)for(const type of w.types)for(const[id,n]of Object.entries(mobDrops[type]))enemyDrops[id]=(enemyDrops[id]||0)+w.count/w.types.length*n;
 return {name:p.split('/').pop(),rewards,arenaPath,points,spawns,mining,enemyDrops,waves,total,required,earliestThresholdSpawnSeconds:earliest,lastSpawnSeconds:t-12,expectedTotalHp:hp,expectedSingleTargetHits:hits};
});
const tutorialRewards={...levels[0].rewards};
for(const [id,amount]of Object.entries(buildings.LumberjackBuilding[0].cost))tutorialRewards[id]=Math.max(tutorialRewards[id]||0,amount);
for(const [id,amount]of Object.entries(items.Planks.cost))tutorialRewards[id]=Math.max(tutorialRewards[id]||0,(buildings.LumberjackBuilding[0].cost[id]||0)+amount);

function expand(targetItems,requiredBuildings){
 const needs={...targetItems},built={},left={},raw={},jobs={};
 function needBuilding(name,level){while((built[name]||0)<level){const index=built[name]||0;built[name]=index+1;for(const [id,n]of Object.entries(buildings[name][index].cost))need(id,n);}}
 function need(id,n){const used=Math.min(left[id]||0,n);left[id]=(left[id]||0)-used;n-=used;if(n<=0)return;
  const item=items[id];if(!item)throw Error('Unknown item '+id);
  if(!Object.keys(item.cost).length){raw[id]=(raw[id]||0)+n;return;}
  if(owners[id])needBuilding(...owners[id]);
  const batches=Math.ceil(n/item.output);jobs[id]=(jobs[id]||0)+batches;left[id]+=batches*item.output-n;
  for(const [input,amount]of Object.entries(item.cost))need(input,amount*batches);
 }
 for(const [name,level]of Object.entries(requiredBuildings))needBuilding(name,level);
 for(const [id,n]of Object.entries(needs))need(id,n);
 return {raw,buildings:built,jobs,craftJobs:Object.values(jobs).reduce((a,b)=>a+b,0),craftMinutes:Object.entries(jobs).reduce((a,[id,n])=>a+n*items[id].seconds,0)/60};
}
// Resolve item IDs from filenames (the sword IDs omit the Item suffix).
const idByFile=Object.fromEntries(Object.entries(items).map(([id,item])=>[item.path.split('/').pop().replace('.asset',''),id]));
function kit(tier){return Object.fromEntries([...(tier==='Netherite'?['NetheritheSwordItem','NetherithePickaxe']:[tier+'SwordItem',tier+'Pickaxe']),...[ 'Helmet','Chestplate','Leggings','Boots'].map(s=>tier==='Diamond'&&s==='Chestplate'?'DiamondArmor':tier+s)].map(f=>{if(!idByFile[f])throw Error(f);return [idByFile[f],1]}));}
const allBuildings=Object.fromEntries(Object.entries(buildings).map(([id,list])=>[id,list.length]));
const milestones={iron:expand(kit('Iron'),{}),diamond:expand({...kit('Iron'),...kit('Diamond')},{}),netherite:expand({...kit('Iron'),...kit('Diamond'),...kit('Netherite')},allBuildings),buildings:expand({},allBuildings)};
// Aggregate optimistic trip allocation. Unlocking is assumed possible; not an executable progression route.
// Ore mining is an expectation at the stated fraction of resource points. No mob drops, ads or gifts.
function allocate(raw,miningFraction,maxLevel=3,bonus=0){
 const mergeCategories=r=>{const result={};for(const [id,n]of Object.entries(r)){const key=items[id]?.category||id;result[key]=(result[key]||0)+n;}return result;};
 raw=mergeCategories(raw);
 const yields=levels.map((l,i)=>{const r={};for(const [id,n]of Object.entries(l.rewards))r[id]=n+Math.floor(n*bonus/100);for(const [id,n]of Object.entries(l.mining))r[id]=(r[id]||0)+n*miningFraction;return mergeCategories(r);});
 const farm={},needed={};for(const [id,n]of Object.entries(raw)){if(!yields.some((r,i)=>i<=maxLevel&&(r[id]||0)>0))farm[id]=n;else needed[id]=n;}
 const mins=[1,maxLevel>=1?1:0,maxLevel>=2?1:0,maxLevel>=3?1:0];
 for(const [id,n]of Object.entries(needed)){const sources=yields.map((r,i)=>i<=maxLevel&&r[id]>0?i:-1).filter(i=>i>=0);if(sources.length===1)mins[sources[0]]=Math.max(mins[sources[0]],Math.ceil(n/yields[sources[0]][id]-1e-8));}
 let best=null; const ids=Object.keys(needed);
 for(let f=mins[3];f<=mins[3]+(maxLevel>=3?40:0);f++)for(let m=mins[2];m<=mins[2]+(maxLevel>=2?80:0);m++)for(let s=mins[1];s<=mins[1]+(maxLevel>=1?40:0);s++){
  if(best&&f+m+s+mins[0]>=best.trips)continue;
  let v=mins[0],valid=true;
  for(const id of ids){const deficit=needed[id]-f*(yields[3][id]||0)-m*(yields[2][id]||0)-s*(yields[1][id]||0);if(deficit<=1e-7)continue;if(!(yields[0][id]>0)){valid=false;break;}v=Math.max(v,Math.ceil(deficit/yields[0][id]-1e-8));}
  if(valid&&(!best||v+s+m+f<best.trips))best={runs:[v,s,m,f],trips:v+s+m+f};
 }
 return {...best,farmOnly:farm};
}
for(const [name,milestone]of Object.entries(milestones))milestone.allocations=Object.fromEntries([0,.5,1].map(f=>[f,allocate(milestone.raw,f,name==='iron'?1:3)]));
// A concrete cautious route, unlike the aggregate lower bound: iron before mine,
// diamond sword before forest, then full diamond, all buildings and full netherite.
function route(fraction,runMinutes,lootFraction=.75){
 const inventory={},built={},owned={},runs=[0,0,0,0],checkpoints={};let minutes=0,jobs=0,allowed=0,craftMinutes=0;
 const key=id=>items[id]?.category||id;
 function advance(dt){minutes+=dt;const farm=built.FarmBuilding||0;if(farm){const wheatPerMinute=[0,1.5,4/1.5,6][farm];inventory[key('Wheat')]=(inventory[key('Wheat')]||0)+dt*wheatPerMinute;}}
 function trip(i,tutorial=false){runs[i]++;advance(tutorial?5:runMinutes[i]);const l=levels[i],bonus=(built.StorageBuilding||0)*5;
  for(const[id,n]of Object.entries(tutorial?tutorialRewards:l.rewards))inventory[key(id)]=(inventory[key(id)]||0)+n+Math.floor(n*bonus/100);
  if(!tutorial)for(const[id,n]of Object.entries(l.mining))inventory[key(id)]=(inventory[key(id)]||0)+n*fraction;
  if(!tutorial)for(const[id,n]of Object.entries(l.enemyDrops))inventory[key(id)]=(inventory[key(id)]||0)+n*lootFraction;
 }
 function build(name,level){while((built[name]||0)<level){const index=built[name]||0;for(const[id,n]of Object.entries(buildings[name][index].cost))need(id,n);built[name]=index+1;advance(.35);}}
 function need(id,n){const k=key(id),item=items[id];if(!item)throw Error(id);
  if((inventory[k]||0)+1e-7>=n){inventory[k]-=n;return;}
  if(Object.keys(item.cost).length){if(owners[id])build(...owners[id]);while((inventory[k]||0)+1e-7<n){const count=item.stackable?Math.ceil((n-(inventory[k]||0))/item.output):1;for(const[input,q]of Object.entries(item.cost))need(input,q*count);inventory[k]=(inventory[k]||0)+item.output*count;advance(item.seconds/60);craftMinutes+=item.seconds/60;jobs++;}}
  else if(id==='Wheat'){if(!built.FarmBuilding)build('FarmBuilding',1);advance((n-(inventory[k]||0))/[0,1.5,4/1.5,6][built.FarmBuilding]);}
  else{let best=-1,bestRate=0;for(let i=0;i<=allowed;i++){const l=levels[i];let amount=0;for(const[x,q]of Object.entries(l.rewards))if(key(x)===k)amount+=q+Math.floor(q*(built.StorageBuilding||0)*.05);for(const[x,q]of Object.entries(l.mining))if(key(x)===k)amount+=q*fraction;for(const[x,q]of Object.entries(l.enemyDrops))if(key(x)===k)amount+=q*lootFraction;if(amount/runMinutes[i]>bestRate){best=i;bestRate=amount/runMinutes[i];}}if(best<0)throw Error('Unavailable '+id);let guard=0;while((inventory[k]||0)+1e-7<n){trip(best);if(++guard>1000)throw Error('Trip overflow');}}
  inventory[k]=(inventory[k]||0)-n;
 }
 const checkpoint=name=>checkpoints[name]={minutes:Number(minutes.toFixed(1)),runs:[...runs],trips:runs.reduce((a,b)=>a+b,0),craftJobs:jobs,craftMinutes:Number(craftMinutes.toFixed(1))};
 const equip=ids=>{for(const id of Object.keys(ids))if(!owned[id]){need(id,1);owned[id]=true;}};
 trip(0,true);build('LumberjackBuilding',1);need('Planks',1);checkpoint('tutorial');
 need(idByFile.StoneSwordItem,1);allowed=1;trip(1);equip(kit('Iron'));checkpoint('iron');
 allowed=2;trip(2);need(idByFile.DiamondSwordItem,1);owned[idByFile.DiamondSwordItem]=true;checkpoint('forestReady');
 allowed=3;trip(3);checkpoint('allLevelsCompleted');equip(kit('Diamond'));checkpoint('diamond');
 // Farm early lets its wheat accumulate during the remaining resource trips.
 build('FarmBuilding',1);build('StorageBuilding',3);
 for(const[name,level]of Object.entries(allBuildings))build(name,level);checkpoint('allBuildings');
 equip(kit('Netherite'));checkpoint('netherite');return {fraction,lootFraction,runMinutes,checkpoints};
}
const routes={rush:route(0,[2,2.5,3,4]),gatherer:route(.5,[4,5,6,7]),thorough:route(1,[6,7,8,10])};
const output={assumptions:{combat:'Single-target hit counts only, no animation/navigation/dodge/survival simulation',economy:'Aggregate crafting dependencies with batch rounding. No ads, daily rewards, death losses, enemy drops, or equipment salvage. Allocations minimize trips, not time; progression order is not enforced. Starter kit and tutorial reward are not deducted. Wheat supplied by passive farm.'},levels,mobs,weapons,armor:Object.fromEntries(['Iron','Golden','Diamond','Netherite'].map(t=>[t,Object.entries(items).filter(([id])=>id.startsWith(t)).reduce((a,[,x])=>a+x.resist,0)])),milestones};
output.routes=routes;
output.assumptions.routes='Sequential cautious gearing route; full clear, 75% expected enemy loot collected. Expected mining yields, not random rolls. Per-run minutes are explicit assumptions including travel/UI/loading, all crafting waits charged separately (no overlap), no failures or ads/gifts. Farm uses average wheat rate; one-time tutorial assumed 5 minutes. Materials are batched in the needed quantity (idealized grouping; real All may spend extra input and create surplus). Aggregate allocations above exclude enemy loot and do not enforce route order; their craftJobs/craftMinutes remain single-recipe batch totals, not bulk-job times.';
fs.writeFileSync('Documentation/playtime-simulation.json',JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify({levels:levels.map(l=>({name:l.name,mobs:l.total,kills:l.required,hp:l.expectedTotalHp,spawn:l.earliestThresholdSpawnSeconds,points:l.points,rewards:l.rewards,mining:l.mining,spawns:l.spawns.map(s=>[s.path.split('/').pop(),s.probability])})),weapons,armor:output.armor,milestones},null,2));

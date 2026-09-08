// Rebuild the complete balance and then apply production/visual additions.
const {execFileSync}=require('child_process');
for(const script of ['apply-balance.cjs','production-assets.cjs','wire-production-art.cjs','apply-resource-balance.cjs','validate-balance.cjs'])
 execFileSync(process.execPath,['Documentation/'+script],{stdio:'inherit'});

import sys, re, collections
import xml.etree.ElementTree as ET
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
root=ET.parse(sys.argv[1]).getroot()
rows=[]
for r in root.iter('{%s}UnitTestResult'%ns['t']):
    if r.get('outcome')!='Failed': continue
    name=r.get('testName'); msg=''
    m=r.find('.//t:Message',ns)
    if m is not None: msg=(m.text or '')
    rows.append((name,msg))
def kind(msg):
    first=msg.strip().splitlines()[0] if msg.strip() else ''
    if 'No exception was thrown' in msg or 'but the test passed' in msg or 'Expected' in first and 'but the test passed' in msg: return 'NOW-PASSES (Fails wrapper no longer needed)'
    if 'ThrowsAny() Failure' in msg: return 'NOW-PASSES (ThrowsAny: none thrown)'
    if 'Assert.Throws() Failure' in msg or 'Expected' in msg and 'Actual' in msg and 'Exception' in first: return 'DIFFERENT EXCEPTION'
    if 'AssertMql' in msg or 'Expected:' in msg and '$' in msg: return 'MQL DIFFERS'
    return 'OTHER: '+re.sub(r'\d+','N',first)[:110]
c=collections.Counter(kind(m) for _,m in rows)
for k,v in c.most_common(25): print(v,k)
cls=collections.Counter(re.sub(r'\.[^.(]+(\(.*)?$','',n).split('.')[-1] for n,_ in rows)
print('--- by class'); 
for k,v in cls.most_common(25): print(v,k)

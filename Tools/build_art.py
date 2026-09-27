"""Build editable, authored street assemblies and an articulated Kiki/Jiji model.

Run with Blender 4.5: blender -b -t 4 --python Tools/build_art.py
Coordinates in helper calls use Unity convention: x/right, y/up, z/north.
The FBX files and .blend sources are original project geometry. No film pixels.
"""
import bpy, bmesh, math, json, random, sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/Koriko/Art'
SOURCE = ROOT / 'art-source'
PREVIEWS = ROOT / 'Docs/previews'
for path in (ART, SOURCE, PREVIEWS): path.mkdir(parents=True, exist_ok=True)
random.seed(20327)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1
ATLAS = bpy.data.images.load(str(ART / 'PaintedSurfaces.png'))
MATERIALS = {}
SHAPES = {}
CHUNK = 'Town'
PALETTE=[(.89,.82,.67),(.74,.52,.45),(.77,.61,.37),(.56,.67,.52),(.55,.27,.18),(.28,.37,.43),(.32,.25,.17),(.67,.64,.53),(.46,.46,.39),(.40,.53,.26),(.23,.36,.20),(.43,.31,.18),(.95,.91,.81),(.10,.14,.23),(.68,.11,.13),(.79,.62,.29)]

def xyz(v): return (v[0], -v[2], v[1])
def mat(name, color, tile=None):
    if tile is not None:color=PALETTE[tile]
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    nodes = m.node_tree.nodes; nodes.clear(); links = m.node_tree.links
    out = nodes.new('ShaderNodeOutputMaterial')
    diffuse = nodes.new('ShaderNodeBsdfDiffuse'); diffuse.inputs['Color'].default_value = (1,1,1,1)
    toon = nodes.new('ShaderNodeShaderToRGB'); links.new(diffuse.outputs[0], toon.inputs[0])
    ramp = nodes.new('ShaderNodeValToRGB'); ramp.color_ramp.interpolation = 'EASE'
    ramp.color_ramp.elements[0].position = .08; ramp.color_ramp.elements[0].color = (.48,.56,.65,1)
    ramp.color_ramp.elements[1].position = .8; ramp.color_ramp.elements[1].color = (1,.98,.87,1)
    links.new(toon.outputs[0],ramp.inputs[0])
    multiply = nodes.new('ShaderNodeMixRGB'); multiply.blend_type='MULTIPLY'; multiply.inputs[0].default_value=1
    multiply.inputs[1].default_value=(*color,1); links.new(ramp.outputs[0],multiply.inputs[2])
    if tile is not None:
        uv = nodes.new('ShaderNodeTexCoord'); half=nodes.new('ShaderNodeVectorMath');half.operation='SCALE';half.inputs[3].default_value=.5;links.new(uv.outputs['UV'],half.inputs[0])
        frac = nodes.new('ShaderNodeVectorMath'); frac.operation='FRACTION';links.new(half.outputs[0],frac.inputs[0])
        twice=nodes.new('ShaderNodeVectorMath');twice.operation='SCALE';twice.inputs[3].default_value=2;links.new(frac.outputs[0],twice.inputs[0])
        minus=nodes.new('ShaderNodeVectorMath');minus.operation='SUBTRACT';minus.inputs[1].default_value=(1,1,1);links.new(twice.outputs[0],minus.inputs[0])
        absolute=nodes.new('ShaderNodeVectorMath');absolute.operation='ABSOLUTE';links.new(minus.outputs[0],absolute.inputs[0])
        mirror=nodes.new('ShaderNodeVectorMath');mirror.operation='SUBTRACT';mirror.inputs[0].default_value=(1,1,1);links.new(absolute.outputs[0],mirror.inputs[1])
        scale=nodes.new('ShaderNodeVectorMath');scale.operation='SCALE';scale.inputs[3].default_value=.246
        links.new(mirror.outputs[0],scale.inputs[0])
        add=nodes.new('ShaderNodeVectorMath');add.operation='ADD';add.inputs[1].default_value=((tile%4)*.25+.002,(3-tile//4)*.25+.002,0)
        links.new(scale.outputs[0],add.inputs[0])
        tex=nodes.new('ShaderNodeTexImage');tex.image=ATLAS;tex.interpolation='Linear';links.new(add.outputs[0],tex.inputs[0])
        painted=nodes.new('ShaderNodeMixRGB');painted.inputs[0].default_value=.4 if tile in [9,10] else .65;painted.inputs[1].default_value=(*color,1);links.new(tex.outputs[0],painted.inputs[2]);links.new(painted.outputs[0],multiply.inputs[1])
    emit=nodes.new('ShaderNodeEmission');links.new(multiply.outputs[0],emit.inputs[0]);links.new(emit.outputs[0],out.inputs[0])
    MATERIALS[name]=m
    return m

for i in range(16): mat('Paint_'+str(i),(1,1,1),i)
for name,color in {
    'Ink':(.075,.089,.112),'Skin':(.95,.73,.54),'Hair':(.105,.073,.064),
    'Dress':(.095,.10,.17),'Bow':(.66,.09,.105),'Shoe':(.65,.22,.12),
    'White':(.97,.91,.74),'Eye':(.26,.12,.075),'Sea':(.14,.39,.45),
    'Foam':(.7,.84,.79),'Leaf':(.26,.41,.20),'LeafLight':(.44,.56,.28),
    'Flower':(.83,.42,.42),'Lavender':(.53,.48,.63),'Gold':(.88,.65,.24),
    'Glass':(.19,.32,.35),'Distant':(.34,.48,.49),'Cloud':(.92,.94,.86)
}.items(): mat(name,color)

def finish(obj,name,material,parent=None):
    obj.name=name;obj.data.materials.append(MATERIALS[material])
    obj['chunk']=CHUNK
    if parent: obj.parent=parent
    return obj

def box(name,pos,size,material,parent=None,bevel=0):
    vertices=[(-.5,-.5,-.5),(.5,-.5,-.5),(.5,.5,-.5),(-.5,.5,-.5),(-.5,-.5,.5),(.5,-.5,.5),(.5,.5,.5),(-.5,.5,.5)]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(3,7,6,2),(0,4,7,3),(1,2,6,5)]
    obj=mesh(name,[(v[0]*size[0],v[1]*size[1],v[2]*size[2]) for v in vertices],faces,material,parent)
    obj.location=xyz(pos)
    return obj

def ellipsoid(name,pos,size,material,parent=None,segments=16,rings=10):
    key=(segments,rings)
    if key not in SHAPES:
        bm=bmesh.new();bmesh.ops.create_uvsphere(bm,u_segments=segments,v_segments=rings,radius=1)
        data=bpy.data.meshes.new('Sphere');bm.to_mesh(data);bm.free()
        for p in data.polygons:p.use_smooth=True
        SHAPES[key]=data
    obj=bpy.data.objects.new(name,SHAPES[key].copy());bpy.context.collection.objects.link(obj)
    obj.location=xyz(pos);obj.scale=(size[0],size[2],size[1])
    return finish(obj,name,material,parent)

def mesh(name,vertices,faces,material,parent=None):
    data=bpy.data.meshes.new(name);data.from_pydata([xyz(v) for v in vertices],[],faces);data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj)
    return finish(obj,name,material,parent)

def cylinder(name,a,b,radius,material,parent=None,vertices=10,radius2=None):
    av=Vector(xyz(a));bv=Vector(xyz(b));delta=bv-av
    if delta.length<.0001:return None
    bm=bmesh.new();bmesh.ops.create_cone(bm,cap_ends=True,cap_tris=False,segments=vertices,radius1=radius,radius2=radius if radius2 is None else radius2,depth=delta.length)
    data=bpy.data.meshes.new(name);bm.to_mesh(data);bm.free()
    if material=='Skin':
        for polygon in data.polygons:
            if len(polygon.vertices)==4:polygon.use_smooth=True
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj)
    obj.location=(av+bv)/2;obj.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return finish(obj,name,material,parent)

def empty(name,pos=(0,0,0),parent=None):
    obj=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(obj);obj.location=xyz(pos)
    if parent:obj.parent=parent
    return obj

def height(x,z):
    def smooth(v):
        t=max(0,min(1,v));return t*t*(3-2*t)
    town=7*smooth((z-56)/47)
    north=smooth((z-150)/150)
    sides=smooth((abs(x)-165)/200)*smooth((z+60)/150)
    ridge=.75+.16*math.sin(x*.018)+.09*math.sin(z*.017)
    return town+(42*north+24*sides)*ridge

def face_up(obj):
    # Open height fields have no enclosed volume; Blender's normal recalculation
    # can choose the underside after their outline changes. Keep terrain walkable.
    if sum(p.normal.z for p in obj.data.polygons)<0:
        bm=bmesh.new();bm.from_mesh(obj.data)
        bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free();obj.data.update()
    return obj

def roof(name,cx,y,cz,w,d,rise,tile,parent=None):
    vertices=[(cx-w/2,y,cz-d/2),(cx+w/2,y,cz-d/2),(cx+w/2,y,cz+d/2),(cx-w/2,y,cz+d/2),(cx,y+rise,cz-d/2),(cx,y+rise,cz+d/2)]
    return mesh(name,vertices,[(0,1,4),(3,5,2),(0,4,5,3),(1,2,5,4),(0,3,2,1)],tile,parent)

def paint_uv(obj,scale=3):
    if obj.type!='MESH':return
    uv=obj.data.uv_layers.new(name='Paint coordinates')
    for face in obj.data.polygons:
        n=face.normal; axis=max(range(3),key=lambda i:abs(n[i]));u,v=[i for i in range(3) if i!=axis]
        for li in face.loop_indices:
            co=obj.matrix_world @ obj.data.vertices[obj.data.loops[li].vertex_index].co
            uv.data[li].uv=(co[u]/scale,co[v]/scale)

def window(name,x,y,z,w=1.5,h=2,front=-1,shutters=True):
    box(name+' dark frame',(x,y,z),(w+.22,h+.22,.17),'Paint_6')
    box(name+' glass',(x,y,z+front*.1),(w,h,.08),'Glass')
    box(name+' mullion',(x,y,z+front*.16),(.065,h,.08),'White')
    box(name+' crossbar',(x,y+.22,z+front*.16),(w,.065,.08),'White')
    box(name+' sill',(x,y-h/2-.13,z+front*.14),(w+.5,.18,.35),'Paint_7')
    if shutters:
        for side in [-1,1]:
            box(name+' shutter',(x+side*(w*.7+.1),y,z),(w*.35,h,.12),'Paint_3')
            for q in [-.42,.0,.42]:box(name+' shutter slat',(x+side*(w*.7+.1),y+q*h,z+front*.08),(w*.32,.045,.06),'White')

BUILDINGS=[]
def building(name,x,z,w,d,h,paint=0,style='shop',front=-1):
    global CHUNK
    y=height(x,z);CHUNK=name
    BUILDINGS.append(dict(name=name,x=x,y=y,z=z,width=w,depth=d,height=h,style=style))
    box(name+' plaster',(x,y+h/2,z),(w,h,d),'Paint_'+str(paint))
    box(name+' foundation',(x,y+.45,z),(w+.15,.9,d+.15),'Paint_7')
    tile='Paint_5' if style in ['townhouse','harbor'] else 'Paint_4'
    roof(name+' pitched roof',x,y+h,z,w+1.1,d+1.2,h*.28,tile)
    box(name+' front cornice',(x,y+h-.1,z+front*(d/2+.12)),(w+.4,.28,.35),'White')
    for side in [-1,1]:
        cylinder(name+' gutter',(x+side*(w/2+.4),y+h-.2,z-d/2-.4),(x+side*(w/2+.4),y+h-.2,z+d/2+.4),.09,'Ink')
        cylinder(name+' downpipe',(x+side*(w/2-.2),y+.25,z+front*(d/2+.22)),(x+side*(w/2-.2),y+h,z+front*(d/2+.22)),.075,'Ink')
    count=max(2,int(w/3.6));floors=max(1,int(h/3.7))
    for side in [-1,1]:
        for level in range(floors):
            for dz in [-d*.25,d*.25]:
                wx=x+side*(w/2+.08);wy=y+2.1+level*3.4;wz=z+dz
                box(name+' side frame',(wx,wy,wz),(.17,2.12,1.57),'Paint_6')
                box(name+' side glass',(wx+side*.1,wy,wz),(.08,1.9,1.35),'Glass')
                box(name+' side mullion',(wx+side*.16,wy,wz),(.08,1.9,.07),'White')
                box(name+' side transom',(wx+side*.16,wy+.22,wz),(.08,.07,1.35),'White')
    for face in [-1,1]:
        for level in range(floors):
            for col in range(count):
                wx=x+(col-(count-1)/2)*(w/(count+.3));wy=y+2.1+level*3.4
                if face==front and level==0 and col==count//2:continue
                window(name+' window',wx,wy,z+face*(d/2+.04),w=1.25 if w<12 else 1.65,h=1.9,front=face,shutters=(level>0))
                if face==front and level==1 and col%2==0:
                    box(name+' flower box',(wx,wy-1.15,z+face*(d/2+.3)),(2,.3,.5),'Paint_6')
                    for a in range(5):ellipsoid(name+' geranium',(wx+(a-2)*.32,wy-.85,z+face*(d/2+.3)),(.25,.2,.25),'Flower',segments=8,rings=6)
    doorx=x+(count//2-(count-1)/2)*(w/(count+.3));doorz=z+front*(d/2+.12)
    box(name+' doorframe',(doorx,y+1.4,doorz),(1.95,2.8,.18),'Paint_6')
    box(name+' door',(doorx,y+1.3,doorz+front*.11),(1.55,2.55,.08),'Paint_3' if style!='bakery' else 'Paint_6')
    box(name+' door glass',(doorx,y+1.7,doorz+front*.17),(1.08,1.0,.05),'Glass')
    ellipsoid(name+' handle',(doorx+.53,y+1.1,doorz+front*.23),(.065,.065,.065),'Gold',segments=8,rings=6)
    box(name+' step',(doorx,y+.1,doorz+front*.55),(2.4,.2,1),'Paint_7')
    if style in ['shop','bakery']:
        awning_z=z+front*(d/2+1.2)
        for stripe in range(10):
            sx=x+(stripe-4.5)*w/10
            ob=box(name+' striped awning',(sx,y+3.4,awning_z),(w/10,.13,2.3),'White' if stripe%2 else ('Bow' if style=='bakery' else 'Paint_3'))
            ob.rotation_euler[0]=front*.15
            box(name+' awning scallop',(sx,y+3.12,awning_z+front*1.05),(w/10,.38,.12),'White' if stripe%2 else ('Bow' if style=='bakery' else 'Paint_3'))
    # Roof silhouettes vary by functional chimneys and dormers.
    box(name+' chimney',(x+w*.27,y+h+h*.18,z+d*.2),(.95,2.7,.95),'Paint_7')
    box(name+' chimney lip',(x+w*.27,y+h+h*.18+1.4,z+d*.2),(1.2,.2,1.2),'Paint_7')
    if w>13:
        box(name+' dormer',(x-w*.15,y+h+.65,z+front*d*.2),(2.7,1.6,2.2),'Paint_'+str(paint))
        roof(name+' dormer roof',x-w*.15,y+h+1.45,z+front*d*.2,3,2.4,.7,tile)
    # Simple colliders preserve generous flight space around decorative trim.
    c=box('COL_'+name,(x,y+h/2,z),(w,h,d),'Ink');c.hide_render=True
    c=roof('COL_Roof_'+name,x,y+h,z,w,d,h*.28,'Ink');c.hide_render=True

def path(name,points,width,tile='Paint_8'):
    global CHUNK
    CHUNK='Streets';vertices=[];faces=[]
    for a,b in zip(points,points[1:]):
        dx=b[0]-a[0];dz=b[1]-a[1];dist=math.hypot(dx,dz);steps=max(1,int(dist/3))
        nx=-dz/dist*width/2;nz=dx/dist*width/2
        for s in range(steps+1):
            t=s/steps;x=a[0]+dx*t;z=a[1]+dz*t
            lift=.085 if tile=='Paint_8' else .07
            vertices.extend([(x+nx,height(x+nx,z+nz)+lift,z+nz),(x-nx,height(x-nx,z-nz)+lift,z-nz)])
            if s<steps:
                k=len(vertices)-2;faces.append((k,k+1,k+3,k+2))
    return face_up(mesh(name,vertices,[tuple(reversed(f)) for f in faces],tile))

def tree(name,x,z,size=1,kind='broad'):
    y=height(x,z);crown=7*size
    cylinder(name+' trunk',(x,y,z),(x+.25*size,y+crown*.82,z),.34*size,'Paint_6',vertices=9,radius2=.19*size)
    if kind=='cypress':
        for i in range(3):ellipsoid(name+' narrow crown',(x,y+size*(4+i*2),z),(size*(1.5-i*.3),size*2.5,size*1.45),'Paint_10',segments=12,rings=8)
    else:
        for dx,dy,dz,s in [(-1.9,-.6,0,1),(.9,.25,.25,1.2),(0,.05,-1.6,1),(1.7,-.5,1.3,.9),(-.4,1.25,.5,.85)]:
            ellipsoid(name+' leafy mass',(x+dx*size,y+crown+dy*size,z+dz*size),(2.5*size*s,2.2*size*s,2.5*size*s),'Paint_10',segments=12,rings=8)
        for dx,dz in [(-1.5,.5),(1.5,.2)]:cylinder(name+' branch',(x,y+crown*.55,z),(x+dx*size,y+crown,z+dz*size),.14*size,'Paint_6',radius2=.05)

def bed(name,x,z,w,d,color='Flower',y=None):
    if y is None:y=height(x,z)
    box(name+' soil',(x,y+.08,z),(w,.14,d),'Paint_11')
    for dx,dz,sw,sd in [(0,-d/2,w,.18),(0,d/2,w,.18),(-w/2,0,.18,d),(w/2,0,.18,d)]:box(name+' stone edging',(x+dx,y+.18,z+dz),(sw,.28,sd),'Paint_7')
    for i in range(max(3,int(w*d*.4))):
        px=x+random.uniform(-w*.44,w*.44);pz=z+random.uniform(-d*.4,d*.4)
        ellipsoid(name+' shrub',(px,y+.42,pz),(.55,.4,.52),'Leaf',segments=8,rings=6)
        for j in range(3):ellipsoid(name+' flowers',(px+(j-1)*.19,y+.76,pz+random.uniform(-.2,.2)),(.18,.13,.18),color,segments=6,rings=5)

def lamp(x,z):
    y=height(x,z)
    cylinder('Lamp post',(x,y,z),(x,y+4.3,z),.08,'Ink')
    box('Lamp glass',(x,y+4.2,z),(.45,.7,.45),'Gold')
    roof('Lamp roof',x,y+4.6,z,.7,.7,.3,'Ink')
    empty('LampGlow',(x,y+4.2,z))

def bench(x,z):
    y=height(x,z)
    for a in [-.22,0,.22]:box('Bench seat',(x,y+.62,z+a),(2.5,.11,.17),'Paint_6')
    for a in [.9,1.2]:box('Bench back',(x,y+a,z+.33),(2.5,.16,.1),'Paint_6')
    for a in [-.9,.9]:box('Bench iron leg',(x+a,y+.35,z),(.08,.7,.65),'Ink')

def fence(name,a,b):
    length=math.hypot(b[0]-a[0],b[1]-a[1]);steps=max(1,math.ceil(length/4))
    for i in range(steps+1):
        t=i/steps;x=a[0]+(b[0]-a[0])*t;z=a[1]+(b[1]-a[1])*t
        box(name+' post',(x,height(x,z)+.62,z),(.16,1.24,.16),'Paint_6')
    for y in [.55,1.02]:cylinder(name+' rail',(a[0],height(*a)+y,a[1]),(b[0],height(*b)+y,b[1]),.07,'Paint_6',vertices=6)

def cow(name,x,z):
    y=height(x,z)
    ellipsoid(name+' cream body',(x,y+1.05,z),(.55,.48,.92),'Paint_12',segments=16,rings=10)
    ellipsoid(name+' brown patch',(x+.51,y+1.13,z-.15),(.06,.26,.36),'Hair',segments=12,rings=8)
    ellipsoid(name+' head',(x,y+1.17,z+.89),(.30,.35,.37),'Paint_12',segments=14,rings=10)
    ellipsoid(name+' muzzle',(x,y+1.02,z+1.18),(.28,.17,.17),'Flower',segments=12,rings=8)
    for side in [-1,1]:
        ellipsoid(name+' ear',(x+side*.33,y+1.39,z+.91),(.20,.09,.10),'Hair',segments=10,rings=6)
        ellipsoid(name+' eye',(x+side*.25,y+1.31,z+1.06),(.035,.045,.04),'Ink',segments=8,rings=6)
        for dz in [-.57,.51]:
            cylinder(name+' leg',(x+side*.36,y+.17,z+dz),(x+side*.38,y+.90,z+dz),.075,'Paint_12',vertices=8)
            box(name+' hoof',(x+side*.36,y+.10,z+dz),(.18,.19,.22),'Hair')
    cylinder(name+' tail',(x,y+1.12,z-.85),(x+.12,y+.40,z-1.13),.035,'Paint_12',vertices=8)
    ellipsoid(name+' tail tuft',(x+.12,y+.37,z-1.13),(.08,.13,.075),'Hair',segments=8,rings=6)

def world():
    global CHUNK
    CHUNK='Landscape'
    # Dense geometry in the playable district, coarse continuous terrain beyond it.
    # The outer edge sits past the fog range, including every woodland tree root.
    xs=[-700,-560,-430,-330,-265,-215]+[-170+i*5.4 for i in range(67)]+[225,275,345,445,565,700]
    zs=[-76+i*5.2 for i in range(51)]+[204,230,270,325,400,500,620,760]
    vertices=[];faces=[];nx=len(xs);nz=len(zs)
    for iz,z in enumerate(zs):
        for ix,x in enumerate(xs):
            vertices.append((x,height(x,z),z))
            if ix<nx-1 and iz<nz-1:
                a=iz*nx+ix;faces.append((a,a+1,a+1+nx,a+nx))
    ground=face_up(mesh('Ground',vertices,[tuple(reversed(f)) for f in faces],'Paint_9'))
    collision=ground.copy();collision.data=ground.data.copy();collision.name='COL_Ground';bpy.context.collection.objects.link(collision);collision.hide_render=True
    box('Quay seawall',(0,-2,-76),(1400,4,2.3),'Paint_7')
    box('Sea',(0,-2.1,-676),(2000,.1,1200),'Sea')
    for i in range(60):
        x=random.uniform(-240,240);z=random.uniform(-360,-90)
        box('Sea painted glint',(x,-2.02,z),(random.uniform(2,10),.015,.1),'Foam')
    roads=[([-155,0],[151,0],8),([-130,-62],[148,-62],8),([-42,-58],[-42,-4],7),([-42,4],[-42,129],7),([64,-58],[64,-4],7),([64,4],[64,129],7),([-42,78],[140,78],6),([-125,129],[142,129],5)]
    for index,(a,b,w) in enumerate(roads):
        path('Pavement_'+str(index),[a,b],w+3.2,'Paint_7')
        path('Street_'+str(index),[a,b],w)
    box('Market paving',(16,.09,25),(52,.14,44),'Paint_7')
    box('Bakery court',(-113,.10,9),(23,.2,11),'Paint_8')
    box('Harbor court',(121,.1,-55),(22,.2,11),'Paint_8')
    box('Tombo court',(-31,.1,49),(18,.2,11),'Paint_8')
    box('Madame raised garden',(90,5.0,103),(45,4,42),'Paint_7')
    c=box('COL_MadameGarden',(90,5.0,103),(45,4,42),'Ink');c.hide_render=True
    box('Madame garden lawn',(90,7.015,103),(44,.05,41),'Paint_9')
    box('Madame approach',(90,7.1,92),(16,.2,20),'Paint_8')
    path('Madame ramp',[(64,78),(78,83),(90,86)],5,'Paint_7')
    # Each row fronts a continuous street. Roof and facade variation follows lot widths.
    building('Osono_Bakery',-113,23,22,15,8.4,0,'bakery')
    for i,(x,w,h,p) in enumerate([(-144,17,10,2),(-89,22,12,1),(-67,20,9.6,3)]):building('Bakery_Row_'+str(i),x,20,w,17,h,p)
    for i,(x,w,h,p) in enumerate([(-145,18,9,1),(-124,21,12,0),(-101,22,10.5,2),(-77,22,13,0),(-55,16,9,3)]):building('South_Shops_'+str(i),x,-18,w,18,h,p,'shop',1)
    for i,(x,w,h,p) in enumerate([(-18,20,11,1),(5,23,13,0),(30,23,10,2),(51,15,12,3),(80,18,11,0),(101,21,9,1),(124,22,13,2),(146,18,11,0)]):building('Market_Street_'+str(i),x,-19,w,19,h,p,'townhouse',1)
    building('Tombo_Workshop',-31,62,15,12,7,2,'shop')
    building('Madame_House',90,113,25,20,10,1,'townhouse')
    building('Harbor_Post_House',121,-42,22,13,9,3,'harbor')
    for i,(x,w,h,p) in enumerate([(-104,24,10,3),(-77,27,8.5,2),(-18,23,12,0),(8,26,14,1),(35,25,11,0),(83,26,8.5,2)]):building('Quay_Row_'+str(i),x,-46,w,17,h,p,'harbor')
    for i,(x,z,w,d,h,p) in enumerate([(-72,70,24,18,10,0),(-94,87,16,22,9,1),(-112,69,18,17,11,2),(-20,105,25,20,9,0),(11,107,28,23,12,2),(41,106,21,21,10,3),(145,105,23,22,10,0)]):building('Garden_Neighbor_'+str(i),x,z,w,d,h,p,'townhouse')
    # Clock tower and its public square establish a single strong navigation silhouette.
    CHUNK='Clock_Square';x=23;z=36
    box('Clock tower stone',(x,13,z),(7,26,7),'Paint_7')
    for y in [7,17,25]:box('Tower cornice',(x,y,z),(7.6,.35,7.6),'White')
    roof('Tower high slate roof',x,26,z,9,9,9,'Paint_5')
    for face in [-1,1]:
        cylinder('Clock face',(x,22,z+face*3.56),(x,22,z+face*3.69),2,'White',vertices=32)
        for hour in range(12):
            a=hour*math.tau/12
            box('Clock hour',(x+math.sin(a)*1.65,22+math.cos(a)*1.65,z+face*3.75),(.12,.18,.04),'Ink')
        cylinder('Clock long hand',(x,22,z+face*3.8),(x-.9,23.1,z+face*3.8),.06,'Ink')
        cylinder('Clock short hand',(x,22,z+face*3.81),(x+.7,22.25,z+face*3.81),.08,'Ink')
    c=box('COL_ClockTower',(x,17,z),(9,34,9),'Ink');c.hide_render=True
    for x,z in [(-6,33),(41,37),(-7,12),(40,8)]:
        tree('Square linden',x,z,.8);bench(x+3,z);bed('Square garden',x,z,4,4)
    for x,z in [(0,43),(14,46),(39,26)]:
        for dx in [-2,2]:cylinder('Market stall',(x+dx,0,z),(x+dx,3,z),.1,'Paint_6')
        roof('Canvas market cover',x,3,z,5,3,1,'White')
        box('Market table',(x,1,z),(4.6,.18,2),'Paint_6')
        for i in range(9):ellipsoid('Market apples',(x+(i%3-1)*.7,1.3,z+(i//3-1)*.45),(.22,.22,.22),'Flower',segments=8,rings=6)
    # A public garden connects the market's northern edge to the garden lane.
    CHUNK='Market_Garden'
    path('Park circuit',[(-14,54),(52,54),(52,72),(-14,72),(-14,54)],1.8,'Paint_7')
    path('Park entrance',[(20,47),(20,78)],1.8,'Paint_7')
    for x,z in [(-11,57),(4,70),(38,57),(50,70)]:tree('Park linden',x,z,.72)
    for x,z in [(-1,59),(7,68),(33,58),(42,68)]:bed('Park borders',x,z,5,3,'Lavender')
    for x,z in [(-8,68),(31,69),(42,55)]:bench(x,z)
    cylinder('Park fountain rim',(20,height(20,63)+.12,63),(20,height(20,63)+.58,63),2.1,'Paint_7',vertices=24)
    cylinder('Park fountain water',(20,height(20,63)+.59,63),(20,height(20,63)+.62,63),1.78,'Sea',vertices=24)
    cylinder('Park fountain pillar',(20,height(20,63)+.6,63),(20,height(20,63)+1.6,63),.24,'Paint_7',vertices=12)
    # Garden enclosure, usable paths, planted edges and open landing court.
    CHUNK='Madame_Garden'
    for x in [68,112]:
        box('Garden boundary',(x,7.6,103),(.6,1.2,42),'Paint_7')
        for z in [87,100,116]:tree('Garden cypress',x+(-2 if x<90 else 2),z,.85,'cypress')
    for x,z,w,d in [(76,91,7,12),(104,91,7,12),(76,110,8,6),(103,125,10,4)]:bed('Madame roses',x,z,w,d,y=7)
    for dx in [-3,3]:cylinder('Garden arch post',(90+dx,7,82),(90+dx,10,82),.13,'Paint_6')
    for dz in [-.7,.7]:cylinder('Garden pergola',(86.5,10,82+dz),(93.5,10,82+dz),.13,'Paint_6')
    for i in range(9):ellipsoid('Climbing roses',(86.7+i*.8,10.1,82),(.65,.5,.7),'Flower',segments=8,rings=6)
    # Bakery life is grouped by its doors and garden, never across the landing court.
    CHUNK='Bakery_Courtyard'
    bed('Bakery flower bed',-128,10,4,9);bed('Bakery herbs',-99,11,3,8,'Lavender')
    for x in [-125,-100]:
        cylinder('Cafe table pedestal',(x,0,8),(x,.9,8),.1,'Ink')
        cylinder('Cafe round table',(x,.9,8),(x,1,8),.9,'Paint_6',vertices=16)
        bench(x,10)
    for x in [-121,-117,-113]:
        box('Bread crate',(x,.65,15),(2,.5,.8),'Paint_6')
        for q in range(4):ellipsoid('Bread loaf',(x+(q-1.5)*.38,1,15),(.3,.2,.3),'Gold',segments=10,rings=6)
    # Connected private gardens between rows and lanes.
    CHUNK='Courtyard_Gardens'
    for x,z in [(-140,52),(-118,46),(-93,48),(-68,47),(-5,77),(28,76),(137,44),(109,45),(80,46)]:
        tree('Courtyard tree',x,z,.8+random.random()*.25)
        bed('Kitchen garden',x+6,z,4,7,'Lavender')
        box('Low garden wall',(x, .65+height(x,z), z+8),(18,1.3,.5),'Paint_7')
        bench(x-4,z+3)
    CHUNK='Quay_Details'
    for x in range(-123,148,18):
        cylinder('Quay bollard',(x,0,-72),(x,.7,-72),.18,'Ink')
        lamp(x,-68)
    for x in range(-138,148,30):lamp(x,5.4)
    for z in [19,51,91,117]:lamp(69,z)
    # Boats sit below the quay; a readable open water corridor leads to the airship.
    for bx,bz in [(-50,-93),(20,-113),(76,-91)]:
        hull=ellipsoid('Boat hull',(bx,-1.3,bz),(2.8,1.2,7),'Paint_6',segments=16,rings=8)
        box('Boat deck',(bx,-.8,bz),(4.7,.2,11),'Paint_6')
        cylinder('Mast',(bx,-.5,bz),(bx,12,bz),.12,'Paint_6')
        mesh('Sail',[(bx+.2,1,bz),(bx+.2,11,bz),(bx+5,1.4,bz)],[(0,1,2)],'White')
    CHUNK='Airship'
    ellipsoid('Spirit of Freedom',(131,58,-112),(26,8,8),'White',segments=32,rings=16)
    c=ellipsoid('COL_AirshipEnvelope',(131,58,-112),(26,8,8),'Ink',segments=20,rings=12);c.hide_render=True
    for dx in [-15,-7,0,7,15]:
        points=[]
        r=math.sqrt(max(0,1-(dx/26)**2))*8
        for j in range(32):
            a=j*math.tau/32; b=(j+1)*math.tau/32
            cylinder('Airship envelope seam',(131+dx,58+r*math.sin(a),-112+r*math.cos(a)),(131+dx,58+r*math.sin(b),-112+r*math.cos(b)),.035,'Gold')
    box('Airship gondola',(131,42,-112),(17,4,5),'Paint_6')
    box('Airship platform',(126,40,-106),(13,.25,7),'Paint_6')
    c=box('COL_AirshipPlatform',(126,39.8,-106),(13,.4,7),'Ink');c.hide_render=True
    c=box('COL_AirshipGondola',(131,42,-112),(17,4,5),'Ink');c.hide_render=True
    for x in [123,138]:
        cylinder('Airship rigging',(x,44,-113),(x,53,-117),.04,'Ink')
        cylinder('Airship rigging',(x,44,-110),(x,53,-106),.04,'Ink')
    for x in [123,139]:window('Airship cabin',x,43,-109.45,1.5,1.3,front=1,shutters=False)
    mesh('Airship tail',[(105,58,-112),(97,66,-112),(101,55,-112)],[(0,1,2)],'Bow')
    # Perimeter orchards and woodland frame the town rather than filling its streets.
    CHUNK='Orchard'
    for ix in range(5):
        for z in [109,144,157]:tree('Apple tree',-138+ix*13,z,.8)
    path('Orchard walk',[(-140,99),(-140,129),(-125,129)],2,'Paint_7')
    for x in [-152,-76]:
        fence('Orchard fence',(x,101),(x,123));fence('Orchard fence',(x,136),(x,161))
    fence('Orchard north fence',(-152,161),(-76,161))
    CHUNK='Bakery_Pasture'
    for a,b in [((-156,73),(-125,73)),((-125,73),(-125,99)),((-156,73),(-156,99)),((-156,99),(-144,99)),((-137,99),(-125,99))]:fence('Pasture fence',a,b)
    cow('Grazing cow',-145,83);cow('Small pasture cow',-134,91)
    box('Pasture water trough',(-128,height(-128,96)+.35,96),(2.6,.7,1.1),'Paint_7')
    box('Trough water',(-128,height(-128,96)+.72,96),(2.3,.04,.8),'Sea')
    CHUNK='Wooded_Hills'
    for i in range(65):
        x=-177+(i%22)*17+random.uniform(-3,3);z=163+(i//22)*15+random.uniform(-3,3)
        tree('Hillside tree',x,z,1.1+random.random()*.65,'cypress' if i%7==0 else 'broad')
    CHUNK='Background_Groves'
    for cx,cz in [(-170,242),(-45,284),(96,242),(232,277)]:
        for row in range(3):
            for col in range(4):
                x=cx+(col-1.5)*12+random.uniform(-3,3);z=cz+(row-1)*12+random.uniform(-3,3)
                tree('Distant grove',x,z,1.45+random.random()*.5)
    for side in [-1,1]:
        for i in range(10):tree('Town edge tree',side*165, -47+i*18,1.1)
    for d in CatalogDestinations:
        empty('Anchor_'+d['id'],(d['x'],d['y'],d['z']))
    # Export each street/material assembly as one mesh. Colliders and anchors stay separate.
    print('World geometry created; assigning paint coordinates',flush=True)
    bpy.context.view_layer.update()
    for obj in list(bpy.context.scene.objects):
        if obj.type=='MESH':
            material=obj.data.materials[0].name
            paint_uv(obj,22 if material=='Paint_9' else 7 if material=='Paint_10' else 4 if material in ['Paint_0','Paint_1','Paint_2','Paint_3'] else 3)
    consolidate()
    export('KorikoNeighborhood')
    # Artist previews are Blender renders, clearly separate from Unity verification.
    render_preview('neighborhood-aerial',(150,120,-175),(-10,0,30),42)
    render_preview('bakery-street',(-143,5,3),(-107,4,20),36)

CatalogDestinations=[
    dict(id='bakery',x=-113,y=.16,z=9),dict(id='clock',x=10,y=.16,z=18),
    dict(id='harbor',x=121,y=.16,z=-55),dict(id='madame',x=90,y=7.16,z=91),
    dict(id='tombo',x=-31,y=.16,z=49),dict(id='airship',x=126,y=40.16,z=-106)
]

def consolidate():
    groups={}
    for obj in list(bpy.context.scene.objects):
        if obj.type=='MESH' and not obj.name.startswith('COL_'):
            key=(obj.get('chunk','Town'),obj.data.materials[0].name)
            groups.setdefault(key,[]).append(obj)
    for (chunk,material),objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        objects[0].name=chunk+'__'+material

def export(name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.type in ['MESH','EMPTY','ARMATURE']:obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(ART/(name+'.fbx')),use_selection=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,use_mesh_modifiers=True,bake_anim=False,path_mode='STRIP')
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))

def render_preview(name,pos,target,lens=50):
    scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT'
    scene.render.resolution_x=1280;scene.render.resolution_y=800;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.world.use_nodes=True
    scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.52,.70,.76,1)
    scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast'
    bpy.ops.object.light_add(type='SUN',location=(0,0,80));sun=bpy.context.object;sun.data.energy=2.4;sun.rotation_euler=(.6,-.45,-.8);sun.data.angle=.12
    bpy.ops.object.camera_add(location=xyz(pos));camera=bpy.context.object;direction=Vector(xyz(target))-camera.location;camera.rotation_euler=direction.to_track_quat('-Z','Y').to_euler();camera.data.lens=lens
    scene.camera=camera;scene.render.filepath=str(PREVIEWS/(name+'.png'))
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera,do_unlink=True);bpy.data.objects.remove(sun,do_unlink=True)

def kiki():
    global CHUNK
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    CHUNK='Kiki';root=empty('KikiRig');body=empty('Body',(0,1.28,0),root)
    # Independent import anchors: Unity may fold the single rig root differently
    # from the neighborhood's multiple roots. Never reuse the world's correction.
    for name,position in [('Origin',(0,0,0)),('Right',(1,0,0)),('Up',(0,1,0)),('Forward',(0,0,1))]:
        empty('RiderAnchor_'+name,position,root)
    # Parts are authored around named pivots for controllable flight poses.
    def part(name,pos,size,material,parent=body):
        obj=ellipsoid(name,pos,size,material,segments=32,rings=20)
        bpy.context.view_layer.update()
        matrix=obj.matrix_world.copy();obj.parent=parent;obj.matrix_world=matrix
        return obj
    def attach(obj,parent):
        bpy.context.view_layer.update()
        matrix=obj.matrix_world.copy();obj.parent=parent;obj.matrix_world=matrix;return obj
    # A tailored bell dress: broad shoulders, slim waist, asymmetric flared hem.
    rings=[(.72,.43,.33),(.85,.46,.34),(1.12,.31,.23),(1.38,.29,.22),(1.55,.36,.21),(1.66,.24,.16)]
    verts=[]
    for y,rx,rz in rings:
        for i in range(24):
            a=i*math.tau/24;wave=.02*math.cos(5*a)*(1 if y<1 else .2)
            verts.append(((rx+wave)*math.cos(a),y+(.04*math.sin(a) if y<1 else 0),(rz+wave)*math.sin(a)))
    faces=[]
    for j in range(len(rings)-1):
        for i in range(24):a=j*24+i;b=j*24+(i+1)%24;faces.append((a,b,b+24,a+24))
    faces.append(tuple(reversed(range(24))));faces.append(tuple(range(120,144)))
    dress=attach(mesh('Dress',verts,faces,'Dress'),body)
    for p in dress.data.polygons:p.use_smooth=True
    head=empty('Head',(0,1.84,.02));attach(head,body)
    part('Neck',(0,1.67,.02),(.105,.15,.10),'Skin')
    part('Face',(0,1.93,.055),(.275,.32,.25),'Skin',head,)
    part('Left ear',(-.274,1.95,.015),(.055,.08,.045),'Skin',head)
    part('Right ear',(.274,1.95,.015),(.055,.08,.045),'Skin',head)
    # Hair forms a bob around the back and temples, with individually shaped bangs.
    hairverts=[];hairfaces=[]
    for j in range(10):
        polar=.06+j/9*2.08
        for i in range(32):
            a=i*math.tau/32
            px=.302*math.sin(polar)*math.cos(a);pz=.284*math.sin(polar)*math.sin(a)
            py=2.0+.30*math.cos(polar)
            if pz>.09 and py<2.085:py=2.10+.035*math.sin(a*5)
            hairverts.append((px,py,pz-.025))
    for j in range(9):
        for i in range(32):a=j*32+i;b=j*32+(i+1)%32;hairfaces.append((a,b,b+32,a+32))
    hair=attach(mesh('Bob hair',hairverts,hairfaces,'Hair'),head)
    for p in hair.data.polygons:p.use_smooth=True
    for x,y,z,s in [(-.23,1.92,.10,.09),(.23,1.92,.1,.09),(-.17,2.11,.235,.08),(-.06,2.12,.272,.07),(.06,2.13,.276,.08),(.18,2.12,.23,.09)]:part('Hair lock',(x,y,z),(s,.14,.052),'Hair',head)
    for side in [-1,1]:
        part('Eye white',(side*.112,1.99,.267),(.065,.072,.021),'White',head)
        part('Brown iris',(side*.10,1.99,.285),(.027,.05,.013),'Eye',head)
        part('Pupil',(side*.10,1.992,.297),(.014,.035,.006),'Ink',head)
        part('Eye glint',(side*.10-.008,2.009,.302),(.008,.012,.004),'White',head)
        attach(cylinder('Upper eyelid',(side*.112-.057,2.034,.281),(side*.112+.057,2.04,.281),.008,'Ink'),head)
    part('Nose',(0,1.92,.299),(.027,.035,.025),'Skin',head)
    attach(cylinder('Quiet smile',(-.033,1.845,.302),(.033,1.845,.302),.006,'Eye'),head)
    bow=empty('Bow',(0,2.3,.01));attach(bow,head)
    part('Bow knot',(0,2.30,.015),(.055,.08,.065),'Bow',bow)
    for side in [-1,1]:
        ob=part('Bow loop',(side*.18,2.36,-.003),(.18,.13,.055),'Bow',bow);ob.rotation_euler[1]=side*.35
        attach(mesh('Bow tail',[(side*.035,2.32,.035),(side*.22,2.19,.045),(side*.13,2.18,.055)],[(0,1,2)],'Bow'),bow)
    # Legs hang naturally beside the broom, hands reach the handle.
    pivots={}
    for side in [-1,1]:
        label='Left' if side<0 else 'Right'
        leg=empty(label+'Leg',(side*.20,.94,0));attach(leg,body);pivots[label+'Leg']=leg
        attach(cylinder(label+' calf',(side*.24,.80,.08),(side*.27,.30,.05),.065,'Skin',vertices=24,radius2=.052),leg)
        part(label+' shoe',(side*.27,.25,.115),(.085,.07,.16),'Shoe',leg)
        arm=empty(label+'Arm',(side*.29,1.49,.01));attach(arm,body);pivots[label+'Arm']=arm
        part(label+' sleeve',(side*.33,1.43,.06),(.15,.17,.14),'Dress',arm)
        attach(cylinder(label+' forearm',(side*.38,1.35,.13),(side*.15,1.08,.54),.054,'Skin',vertices=24,radius2=.045),arm)
        part(label+' hand',(side*.15,1.075,.54),(.065,.055,.065),'Skin',arm)
    broom=empty('Broom');attach(broom,root)
    attach(cylinder('Broom handle',(0,.97,-1.08),(0,1.07,1.33),.035,'Paint_6',vertices=12),broom)
    strawverts=[];strawfaces=[]
    for z,r in [(-.88,.05),(-1.35,.15),(-1.75,.24)]:
        for i in range(24):
            a=i*math.tau/24;strawverts.append((math.cos(a)*r,.94+math.sin(a)*r,z-.06*math.sin(a*5)))
    for j in range(2):
        for i in range(24):
            a=j*24+i;b=j*24+(i+1)%24;strawfaces.append((a,b,b+24,a+24))
    strawfaces.append(tuple(range(48,72)))
    attach(mesh('Bound broom straw',strawverts,strawfaces,'Paint_15'),broom)
    for i in range(25):
        a=i*2.39996;r=.22*math.sqrt((i+.5)/25)
        attach(cylinder('Broom straw',(0,.97,-.86),(math.cos(a)*r,.92+math.sin(a)*r,-1.75-r*.3),.024,'Gold',vertices=6,radius2=.008),broom)
    attach(cylinder('Broom binding',(0,.97,-.98),(0,.97,-.88),.085,'Bow',vertices=12),broom)
    # Jiji sits behind Kiki, with large pointed ears, a curved tail and bright eyes.
    jiji=empty('Jiji',(0,1.13,-.69));attach(jiji,root)
    part('Jiji body',(0,1.25,-.72),(.13,.19,.15),'Ink',jiji)
    part('Jiji head',(0,1.49,-.68),(.14,.145,.115),'Ink',jiji)
    for side in [-1,1]:
        attach(mesh('Jiji ear',[(side*.04,1.59,-.70),(side*.145,1.76,-.72),(side*.15,1.52,-.64)],[(0,1,2),(2,1,0)],'Ink'),jiji)
        part('Jiji eye',(side*.06,1.51,-.575),(.047,.052,.011),'White',jiji)
        part('Jiji pupil',(side*.059,1.51,-.565),(.012,.035,.005),'Ink',jiji)
    part('Jiji nose',(0,1.465,-.558),(.019,.013,.011),'Flower',jiji)
    tail=[(0,1.14,-.83),(.14,1.10,-1.02),(.24,1.24,-1.1),(.22,1.39,-1.13),(.13,1.44,-1.10)]
    curve=bpy.data.curves.new('Jiji curling tail','CURVE');curve.dimensions='3D';curve.resolution_u=8;curve.bevel_depth=.025;curve.bevel_resolution=3
    spline=curve.splines.new('BEZIER');spline.bezier_points.add(len(tail)-1)
    for point,position in zip(spline.bezier_points,tail):point.co=xyz(position);point.handle_left_type='AUTO';point.handle_right_type='AUTO'
    tail_object=bpy.data.objects.new('Jiji curling tail',curve);bpy.context.collection.objects.link(tail_object)
    bpy.ops.object.select_all(action='DESELECT');tail_object.select_set(True);bpy.context.view_layer.objects.active=tail_object;bpy.ops.object.convert(target='MESH')
    tail_object=bpy.context.object;finish(tail_object,'Jiji curling tail','Ink');attach(tail_object,jiji)
    head.scale*=.86
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH' and not obj.data.uv_layers:paint_uv(obj,.5)
    export('KikiAndJiji')
    render_preview('kiki-model',(3,2.4,4),(0,1.25,-.05),42)
    render_preview('kiki-rear',(-3,2.7,-4),(0,1.25,-.05),42)

if '--character-only' not in sys.argv:
    world()
    resources=ROOT/'Assets/Koriko/Resources';resources.mkdir(parents=True,exist_ok=True)
    (resources/'KorikoLayout.json').write_text(json.dumps(dict(buildings=BUILDINGS,landings=CatalogDestinations),indent=2))
kiki()
print('ART_COMPLETE',len(BUILDINGS),'authored buildings; six delivery courts; articulated Kiki/Jiji; FBX and editable Blender sources')

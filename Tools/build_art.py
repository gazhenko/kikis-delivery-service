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
ATLAS = bpy.data.images.load(str(ART / 'PaintedFilmSurfaces.png'))
ENV_ATLAS = bpy.data.images.load(str(ART / 'EnvironmentSurfaces-v2.png'))
ENV_ROWS = [0,314,628,941,1254]
MATERIALS = {}
SHAPES = {}
CHUNK = 'Town'
PALETTE=[(.89,.82,.67),(.74,.52,.45),(.77,.61,.37),(.56,.67,.52),(.55,.27,.18),(.28,.37,.43),(.32,.25,.17),(.67,.64,.53),(.46,.46,.39),(.40,.53,.26),(.23,.36,.20),(.43,.31,.18),(.95,.91,.81),(.10,.14,.23),(.68,.11,.13),(.79,.62,.29)]

def xyz(v): return (v[0], -v[2], v[1])
def mat(name, color, tile=None,environment_cell=False):
    if tile is not None and not environment_cell:color=PALETTE[tile]
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
        scale=nodes.new('ShaderNodeVectorMath');scale.operation='MULTIPLY'
        top,bottom=ENV_ROWS[tile//4:tile//4+2] if tile<12 or environment_cell else (tile//4*313.5,(tile//4+1)*313.5)
        scale.inputs[1].default_value=(.25-8/1254,(bottom-top-8)/1254,1)
        links.new(mirror.outputs[0],scale.inputs[0])
        add=nodes.new('ShaderNodeVectorMath');add.operation='ADD';add.inputs[1].default_value=((tile%4)*.25+4/1254,1-bottom/1254+4/1254,0)
        links.new(scale.outputs[0],add.inputs[0])
        tex=nodes.new('ShaderNodeTexImage');tex.image=ENV_ATLAS if tile<12 or environment_cell else ATLAS;tex.interpolation='Linear';links.new(add.outputs[0],tex.inputs[0])
        painted=nodes.new('ShaderNodeMixRGB');painted.inputs[0].default_value=.4 if tile in [9,10] else .65;painted.inputs[1].default_value=(*color,1);links.new(tex.outputs[0],painted.inputs[2]);links.new(painted.outputs[0],multiply.inputs[1])
    emit=nodes.new('ShaderNodeEmission');links.new(multiply.outputs[0],emit.inputs[0]);links.new(emit.outputs[0],out.inputs[0])
    MATERIALS[name]=m
    return m

for i in range(16): mat('Paint_'+str(i),(1,1,1),i)
mat('Environment_Brick',(.63,.37,.28),12,True)
mat('Environment_Copper',(.28,.45,.40),13,True)
for name,color in {
    'Ink':(.075,.089,.112),'Skin':(.95,.73,.54),'Hair':(.105,.073,.064),
    'Dress':(.22,.26,.40),'Bow':(.84,.22,.24),'BowShade':(.57,.12,.18),'Shoe':(.65,.22,.12),
    'White':(.97,.91,.74),'Eye':(.26,.12,.075),'Sea':(.14,.39,.45),
    'Foam':(.7,.84,.79),'Leaf':(.26,.41,.20),'LeafLight':(.44,.56,.28),
    'Flower':(.83,.42,.42),'Lavender':(.53,.48,.63),'Gold':(.88,.65,.24),
    'Glass':(.19,.32,.35),'Distant':(.34,.48,.49),'Cloud':(.92,.94,.86)
}.items(): mat(name,color)

# The generated shop paintings have slightly unequal row heights; use inspected
# pixel boundaries rather than letting neighboring artwork bleed into a window.
SHOP_ROWS=[0,332,674,940,1254]
SHOP_ATLAS=bpy.data.images.load(str(ART/'ShopPaintings.png'))
for i in range(8):
    m=mat('Shop_'+str(i),(.9,.86,.74));nodes=m.node_tree.nodes;links=m.node_tree.links
    uv=nodes.new('ShaderNodeTexCoord');scale=nodes.new('ShaderNodeVectorMath');scale.operation='MULTIPLY'
    top,bottom=SHOP_ROWS[i//2:i//2+2]
    scale.inputs[1].default_value=(.5-8/1254,(bottom-top-8)/1254,1)
    links.new(uv.outputs['UV'],scale.inputs[0])
    offset=nodes.new('ShaderNodeVectorMath');offset.operation='ADD';offset.inputs[1].default_value=(i%2*.5+4/1254,1-bottom/1254+4/1254,0);links.new(scale.outputs[0],offset.inputs[0])
    texture=nodes.new('ShaderNodeTexImage');texture.image=SHOP_ATLAS;links.new(offset.outputs[0],texture.inputs[0])
    multiply=next(n for n in nodes if n.type=='MIX_RGB' and n.blend_type=='MULTIPLY')
    links.new(texture.outputs['Color'],multiply.inputs[1])

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

def stroke(name,points,width,material,parent=None):
    """An authored pen line in 3D, used sparingly at architectural/cloth seams."""
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=5
    curve.bevel_depth=width;curve.bevel_resolution=1
    spline=curve.splines.new('BEZIER');spline.bezier_points.add(len(points)-1)
    for point,position in zip(spline.bezier_points,points):
        point.co=xyz(position)
        handle='VECTOR' if 'roof verge' in name or 'glazing bar' in name else 'AUTO'
        point.handle_left_type=handle;point.handle_right_type=handle
    obj=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target='MESH');obj=bpy.context.object
    return finish(obj,name,material,parent)

def paint_tones(obj,character=False):
    """Store broad painted value groups and contour taper in the source mesh."""
    data=obj.data
    colors=data.color_attributes.new(name='Paint tones',type='FLOAT_COLOR',domain='POINT')
    zs=[v.co.z for v in data.vertices];lo=min(zs);span=max(.001,max(zs)-lo)
    material=data.materials[0].name
    for index,v in enumerate(data.vertices):
        t=(v.co.z-lo)/span
        if character:
            # Avoid a mechanical uniform-weight outline around the whole cel.
            alpha=.78+.22*math.sin(t*math.pi)
            colors.data[index].color=(1,1,1,alpha)
        elif material in ['Paint_10','Leaf','LeafLight']:
            colors.data[index].color=(.78+t*.27,.86+t*.20,.87+t*.15,1)
        elif material=='Paint_9':
            co=obj.matrix_world@v.co
            wash=.96+.065*math.sin(co.x*.031+math.sin(co.y*.053)*1.5)+.045*math.sin(co.y*.075+co.x*.012)
            colors.data[index].color=(wash, .97+(wash-.96)*.65, .95+(wash-.96)*.4,1)
        else:
            colors.data[index].color=(.94+t*.07,.96+t*.06,1,1)

def hand_shape_building(name,x,z,y,h):
    """A shared continuous warp keeps windows, walls and roofs aligned."""
    bpy.context.view_layer.update()
    phase=sum(ord(c) for c in name)*.37
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH' or obj.get('chunk')!=name:continue
        matrix=obj.matrix_world.copy();inverse=matrix.inverted()
        for vertex in obj.data.vertices:
            p=matrix@vertex.co;level=max(0,(p.z-y)/h)
            p.x+=level*(.11*math.sin(phase)+.035*math.sin((p.y+z)*.3))
            p.z+=min(1,level)*.085*math.sin((p.x-x)*.31+phase)
            vertex.co=inverse@p
        obj.data.update()

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
    matrix=obj.matrix_world.copy()
    for face in obj.data.polygons:
        n=face.normal; axis=max(range(3),key=lambda i:abs(n[i]));u,v=[i for i in range(3) if i!=axis]
        for li in face.loop_indices:
            co=matrix @ obj.data.vertices[obj.data.loops[li].vertex_index].co
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

def painting(name,x,y,z,w,h,tile):
    vertices=[(x-w/2,y-h/2,z),(x+w/2,y-h/2,z),(x+w/2,y+h/2,z),(x-w/2,y+h/2,z)]
    obj=mesh(name,vertices,[(0,1,2,3),(3,2,1,0)],'Shop_'+str(tile))
    uv=obj.data.uv_layers.new(name='Illustration coordinates');corners=[(0,0),(1,0),(1,1),(0,1)]
    for loop in obj.data.loops:uv.data[loop.index].uv=corners[loop.vertex_index]
    return obj

BUILDINGS=[]
PATHS=[]
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
    count=max(2,int(w/3.6));floors=max(1,1+int((h-3.05)/3.4))
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
                if face==front and level==0 and (col==count//2 or style in ['shop','bakery']):continue
                window(name+' window',wx,wy,z+face*(d/2+.04),w=1.25 if w<12 else 1.65,h=1.9,front=face,shutters=(level>0))
                if face==front and level==1 and col%2==0:
                    box(name+' flower box',(wx,wy-1.15,z+face*(d/2+.3)),(2,.3,.5),'Paint_6')
                    for a in range(5):ellipsoid(name+' geranium',(wx+(a-2)*.32,wy-.85,z+face*(d/2+.3)),(.25,.2,.25),'Flower',segments=8,rings=6)
    doorx=x if style in ['shop','bakery'] else x+(count//2-(count-1)/2)*(w/(count+.3));doorz=z+front*(d/2+.12)
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
        window(name+' attic window',x-w*.15,y+h+.70,z+front*(d*.2+1.13),.95,1.1,front,False)
    # A few painted roof strokes and edge timbers replace a perfectly extruded silhouette.
    for face in [-1,1]:
        rz=z+face*(d/2+.61)
        stroke(name+' drawn roof verge',[(x-w/2-.53,y+h,rz),(x-w*.24,y+h+h*.15+.06,rz),(x+.08,y+h+h*.28,rz),(x+w*.26,y+h+h*.14-.05,rz),(x+w/2+.53,y+h,rz)],.048,'Paint_6')
    stroke(name+' crooked ridge',[(x+.02,y+h+h*.28+.05,z-d/2-.6),(x-.04,y+h+h*.28+.11,z),(x+.05,y+h+h*.28+.05,z+d/2+.6)],.075,tile)
    if style in ['bakery','shop']:
        # Hanging signs and deep shop windows give the street an inhabited human scale.
        sx=x-w*.32;sz=z+front*(d/2+.7)
        cylinder(name+' sign bracket',(sx,y+4.9,sz-front*.6),(sx,y+4.9,sz+front*1.4),.05,'Paint_6')
        box(name+' hanging sign',(sx,y+4.35,sz+front*1.25),(.16,.9,1.25),'Paint_6')
        ellipsoid(name+' sign medallion',(sx,y+4.35,sz+front*1.25),(.10,.31,.45),'Gold',segments=12,rings=8)
        if style=='shop':
            variant=1+sum(ord(c) for c in name)%3
            for wx in [x-w*.29,x+w*.29]:
                window(name+' shop display',wx,y+1.65,z+front*(d/2+.22),min(4.4,w*.30),2.1,front,False)
                if name!='Tombo_Workshop':painting(name+' painted display',wx,y+1.65,z+front*(d/2+.37),min(4.4,w*.30)-.12,1.97,variant)
        sign=4 if style=='bakery' else 5 if name=='Tombo_Workshop' else 7
        box(name+' painted sign frame',(x,y+4.05,z+front*(d/2+.20)),(2.55,1.20,.18),'Paint_6')
        painting(name+' illustrated sign',x,y+4.05,z+front*(d/2+.30),2.38,1.03,sign)
    if style=='bakery':
        # Osono's warm timber shopfront is the first bespoke landmark treatment.
        for wx in [x-w*.35,x+w*.30]:
            window('Bakery display',wx,y+1.6,z+front*(d/2+.22),4.4,2.1,front,False)
            painting('Bakery painted bread',wx,y+1.6,z+front*(d/2+.37),4.27,1.97,0)
            box('Bakery display shelf',(wx,y+.72,z+front*(d/2+.40)),(4.6,.18,.30),'Paint_6')
        box('Bakery fascia',(x,y+3.05,z+front*(d/2+.32)),(w,.44,.22),'Paint_6')
        # An intentionally irregular gable over the door anchors the delivery courtyard.
        box('Bakery central gable',(x,y+h+.6,z+front*(d*.35)),(4.5,2.1,3.9),'Paint_0')
        roof('Bakery central gable roof',x,y+h+1.6,z+front*(d*.35),5.5,4.3,2.1,'Paint_4')
        window('Bakery gable window',x,y+h+.85,z+front*(d*.35+2),1.5,1.8,front,True)
        c=roof('COL_BakeryGable',x,y+h+1.6,z+front*(d*.35),5.5,4.3,2.1,'Ink');c.hide_render=True
    if name=='Harbor_Post_House':
        box('Post house sign frame',(doorx,y+3.7,doorz+front*.10),(2.55,1.25,.17),'Paint_6')
        painting('Post house envelope',doorx,y+3.7,doorz+front*.20,2.38,1.10,6)
    if style=='townhouse' and w>20:
        bx=x-w*.24;bz=z+front*(d/2+.65)
        box(name+' balcony floor',(bx,y+3.9,bz),(4,.20,1.3),'Paint_7')
        cylinder(name+' balcony rail',(bx-2,y+4.85,bz+front*.60),(bx+2,y+4.85,bz+front*.60),.045,'Paint_6')
        for q in range(9):cylinder(name+' balcony spindle',(bx-1.85+q*.46,y+4,bz+front*.60),(bx-1.85+q*.46,y+4.82,bz+front*.60),.026,'Paint_6',vertices=6)
    # Simple colliders preserve generous flight space around decorative trim.
    c=box('COL_'+name,(x,y+h/2,z),(w,h,d),'Ink');c.hide_render=True
    c=roof('COL_Roof_'+name,x,y+h,z,w,d,h*.28,'Ink');c.hide_render=True
    hand_shape_building(name,x,z,y,h)

def path(name,points,width,tile='Paint_8'):
    global CHUNK
    PATHS.append(dict(name=name,width=width,tile=tile,points=[dict(x=p[0],z=p[1]) for p in points]))
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
            canopy=ellipsoid(name+' leafy mass',(x+dx*size,y+crown+dy*size,z+dz*size),(2.5*size*s,2.2*size*s,2.5*size*s),'Paint_10',segments=12,rings=8)
            for vertex in canopy.data.vertices:
                co=vertex.co;co*=1+.075*math.sin(co.x*5+dx)+.045*math.sin(co.y*7+co.z*4)
            canopy.data.update()
        for dx,dz in [(-1.5,.5),(1.5,.2)]:cylinder(name+' branch',(x,y+crown*.55,z),(x+dx*size,y+crown,z+dz*size),.14*size,'Paint_6',radius2=.05)

def flower_head(name,x,y,z,color):
    vertices=[];faces=[]
    for petal in range(5):
        theta=petal*math.tau/5+.18;start=len(vertices)
        for j in range(8):
            a=j*math.tau/8;radial=.75*(.12+math.cos(a)*.15);across=.75*math.sin(a)*.085
            vertices.append((x+math.cos(theta)*radial-math.sin(theta)*across,y+.025*math.cos(a)**2,z+math.sin(theta)*radial+math.cos(theta)*across))
        face=tuple(range(start,start+8));faces.extend([face,tuple(reversed(face))])
    mesh(name+' painted petals',vertices,faces,color)
    ellipsoid(name+' warm flower center',(x,y+.025,z),(.047,.026,.047),'Gold',segments=8,rings=5)

def bed(name,x,z,w,d,color='Flower',y=None):
    if y is None:y=height(x,z)
    box(name+' soil',(x,y+.08,z),(w,.14,d),'Paint_11')
    for dx,dz,sw,sd in [(0,-d/2,w,.18),(0,d/2,w,.18),(-w/2,0,.18,d),(w/2,0,.18,d)]:box(name+' stone edging',(x+dx,y+.18,z+dz),(sw,.28,sd),'Paint_7')
    for i in range(max(3,int(w*d*.4))):
        px=x+random.uniform(-w*.44,w*.44);pz=z+random.uniform(-d*.4,d*.4)
        environment.shrub(name+' flowering bush',px,pz,.62,y=y+.10,flower=color)

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
    # Moving drawn wave strokes now live in the sea shader, with no scattered glint boxes.
    for x in range(-170,180,22):
        stroke('Quay foam stroke',[(x,-2.01,-77.4),(x+7,-2.01,-77.8),(x+15,-2.01,-77.3)],.075,'Foam')
    # Continuous forecourts join both shop rows to their pavements.
    box('Market street forecourts',(0,.025,1),(314,.06,25),'Paint_7')
    roads=[([-155,0],[151,0],8),([-130,-62],[148,-62],8),([-42,-58],[-42,-4],7),([-42,4],[-42,129],7),([64,-58],[64,-4],7),([64,4],[64,129],7),([-42,78],[140,78],6),([-125,129],[142,129],5)]
    for index,(a,b,w) in enumerate(roads):
        path('Pavement_'+str(index),[a,b],w+3.2,'Paint_7')
        path('Street_'+str(index),[a,b],w)
    box('Market paving',(16,.09,25),(52,.14,44),'Paint_7')
    box('Bakery court',(-113,.10,9),(23,.2,11),'Paint_8')
    box('Harbor court',(121,.1,-55),(22,.2,11),'Paint_8')
    box('Tombo court',(-31,.1,49),(18,.2,11),'Paint_8')
    # The raised garden has a real stair opening facing the street, rather than
    # a surface path that disappears through the front retaining wall.
    for index,(cx,cz,sw,sd) in enumerate([(76.75,103,18.5,42),(103.25,103,18.5,42),(90,106,8,36)]):
        box('Madame garden terrace',(cx,5.0,cz),(sw,4,sd),'Paint_7')
        c=box('COL_MadameGarden_'+str(index),(cx,5.0,cz),(sw,4,sd),'Ink');c.hide_render=True
        box('Madame garden lawn',(cx,7.015,cz),(sw-.08,.05,sd-.08),'Paint_9')
    box('Madame approach',(90,7.1,95),(16,.2,14),'Paint_8')
    path('Madame street approach',[(64,78),(90,78),(90,82)],3.4,'Paint_7')
    stair_base=height(90,82)
    for step in range(12):
        top=stair_base+(7.2-stair_base)*(step+1)/12
        box('Madame stone stair',(90,(top+stair_base-.2)/2,82+(step+.5)*.5),(8,top-stair_base+.2,.5),'Paint_7')
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
    for dx in [-3,3]:cylinder('Garden arch post',(90+dx,7,88),(90+dx,10,88),.13,'Paint_6')
    for dz in [-.7,.7]:cylinder('Garden pergola',(86.5,10,88+dz),(93.5,10,88+dz),.13,'Paint_6')
    for i in range(9):
        environment.leafy_mass('Pergola rose foliage',86.7+i*.8,10.05,88,.65,.42,.67,i)
        flower_head('Pergola rose blossom',86.7+i*.8,10.52,88,'Flower')
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
    for i in range(90):
        x=-176+(i%30)*12+random.uniform(-2.4,2.4);z=163+(i//30)*14+random.uniform(-2.5,2.5)
        tree('Hillside tree',x,z,1.1+random.random()*.65,'cypress' if i%7==0 else 'broad')
    CHUNK='Background_Groves'
    for cx,cz in [(-170,242),(-45,284),(96,242),(232,277)]:
        for row in range(3):
            for col in range(4):
                x=cx+(col-1.5)*12+random.uniform(-3,3);z=cz+(row-1)*12+random.uniform(-3,3)
                tree('Distant grove',x,z,1.45+random.random()*.5)
    for side in [-1,1]:
        for i in range(10):tree('Town edge tree',side*165, -47+i*18,1.1)
    environment.finish_world()
    for d in CatalogDestinations:
        empty('Anchor_'+d['id'],(d['x'],d['y'],d['z']))
    # Export each street/material assembly as one mesh. Colliders and anchors stay separate.
    print('World geometry created; assigning paint coordinates',flush=True)
    bpy.context.view_layer.update()
    for obj in list(bpy.context.scene.objects):
        if obj.type=='MESH':
            material=obj.data.materials[0].name
            if not obj.data.uv_layers:paint_uv(obj,72 if material=='Paint_9' else 8 if material=='Paint_10' else 12 if material in ['Paint_0','Paint_1','Paint_2','Paint_3'] else 2.4 if material in ['Paint_4','Paint_5'] else 3)
            paint_tones(obj)
    print('Paint coordinates complete; consolidating spatial material batches',flush=True)
    consolidate()
    print('Spatial batches complete; exporting environment',flush=True)
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
    """Join static world batches without thousands of dependency-graph unlink passes.

    Copy source positions, polygon winding, smoothing, UVs and painted vertex
    colors explicitly, then remove the source IDs together. No character meshes
    or collision meshes use this path.
    """
    groups={}
    for obj in list(bpy.context.scene.objects):
        if obj.type=='MESH' and not obj.name.startswith('COL_'):
            key=(obj.get('chunk','Town'),obj.data.materials[0].name)
            groups.setdefault(key,[]).append(obj)
    removed=[];source_vertices=0;merged_vertices=0;source_faces=0;merged_faces=0
    for (chunk,material),objects in groups.items():
        if len(objects)==1:
            objects[0].name=chunk+'__'+material
            continue
        vertices=[];faces=[];smooth=[];uvs=[];tones=[]
        for obj in objects:
            data=obj.data;matrix=obj.matrix_world.copy();offset=len(vertices)
            vertices.extend(tuple(matrix@v.co) for v in data.vertices)
            faces.extend(tuple(v+offset for v in face.vertices) for face in data.polygons)
            smooth.extend(face.use_smooth for face in data.polygons)
            layer=data.uv_layers.active
            uvs.extend(value for loop in layer.data for value in loop.uv)
            color=data.color_attributes.get('Paint tones')
            tones.extend(value for vertex in color.data for value in vertex.color)
            source_vertices+=len(data.vertices);source_faces+=len(data.polygons)
            removed.append(obj)
        data=bpy.data.meshes.new(chunk+'__'+material)
        data.from_pydata(vertices,[],faces);data.update()
        data.polygons.foreach_set('use_smooth',smooth)
        data.uv_layers.new(name='Paint coordinates').data.foreach_set('uv',uvs)
        data.color_attributes.new(name='Paint tones',type='FLOAT_COLOR',domain='POINT').data.foreach_set('color',tones)
        data.materials.append(MATERIALS[material])
        obj=bpy.data.objects.new(chunk+'__'+material,data);bpy.context.collection.objects.link(obj)
        obj['chunk']=chunk;merged_vertices+=len(data.vertices);merged_faces+=len(data.polygons)
    assert source_vertices==merged_vertices and source_faces==merged_faces,'World batch merge dropped source geometry'
    data_to_remove={obj.data for obj in removed if obj.data.users==1}
    bpy.data.batch_remove(ids=set(removed)|data_to_remove)
    print('WORLD_BATCH_PASS:',len(groups),'material batches;',merged_vertices,'merged vertices;',merged_faces,'merged polygons; UV and paint attributes retained',flush=True)

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
    sys.path.insert(0,str(ROOT/'Tools'))
    from character_model import build_character
    build_character(globals())

sys.path.insert(0,str(ROOT/'Tools'))
import environment_art as environment
environment.install(globals())

if '--character-only' not in sys.argv:
    world()
    resources=ROOT/'Assets/Koriko/Resources';resources.mkdir(parents=True,exist_ok=True)
    (resources/'KorikoLayout.json').write_text(json.dumps(dict(buildings=BUILDINGS,paths=PATHS,landings=CatalogDestinations),indent=2))
if '--world-only' not in sys.argv:kiki()
if '--character-only' in sys.argv:
    print('CHARACTER_ART_COMPLETE: articulated Kiki/Jiji, flight cloth, FBX, editable Blender source and five angle previews')
elif '--world-only' in sys.argv:
    print('ENVIRONMENT_ART_COMPLETE',len(BUILDINGS),'authored frontages and landmarks;',len(PATHS),'connected paths; six delivery courts; character assets preserved')
else:
    print('ART_COMPLETE',len(BUILDINGS),'authored buildings; six delivery courts; articulated Kiki/Jiji; FBX and editable Blender sources')

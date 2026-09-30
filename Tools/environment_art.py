"""Authored environment pass: street frontages, working quay and cultivated edges.

All positions are in the established Unity coordinate system. Detail placement
belongs to a parcel, a garden border or a quay; the six landing circles stay open.
Installed into the existing Blender art pipeline; does not rebuild Kiki.
"""
import math
import random
import bpy
from mathutils import Vector

G = {}
BASE_BUILDING = None
BASE_ROOF = None
FACADE_MATERIAL = 'Paint_0'


def install(g):
    global G, BASE_BUILDING, BASE_ROOF, BASE_TONES, BASE_CONSOLIDATE
    G = g
    BASE_BUILDING = g['building']
    BASE_ROOF = g['roof']
    BASE_TONES = g['paint_tones']
    BASE_CONSOLIDATE = g['consolidate']
    g['building'] = frontage
    g['tree'] = tree
    g['roof'] = roof_form
    g['paint_tones'] = household_tones
    g['consolidate'] = consolidate_keeping_boats


BASE_TONES = None
BASE_CONSOLIDATE = None


def household_tones(obj, character=False):
    """Painted tones, plus a per-window household value in the alpha channel.

    The painted shader lights windows from this value at dusk: most glow warmly,
    some are dim, a few stay dark, and each household lights up at its own moment.
    """
    BASE_TONES(obj, character)
    if character or obj.type != 'MESH' or not obj.data.materials or obj.data.materials[0].name != 'Glass':
        return
    p = obj.matrix_world.translation
    rng = random.Random('%s:%.1f:%.1f:%.1f' % (obj.name.split('.')[0], p.x, p.y, p.z))
    r = rng.random()
    value = .06 if r < .2 else .42 + rng.random() * .28 if r < .5 else .80 + rng.random() * .20
    for c in obj.data.color_attributes.get('Paint tones').data:
        c.color = (c.color[0], c.color[1], c.color[2], value)


def consolidate_keeping_boats():
    # Moored boats stay separate objects so the game can let them ride the swell.
    kept = [o for o in bpy.context.scene.objects if o.get('separate')]
    homes = {o: list(o.users_collection) for o in kept}
    for o in kept:
        for collection in homes[o]:
            collection.objects.unlink(o)
    BASE_CONSOLIDATE()
    for o in kept:
        for collection in homes[o]:
            collection.objects.link(o)
    print('BOATS_KEPT_SEPARATE:', len(kept), 'boat parts', flush=True)


def call(name, *args, **kwargs):
    return G[name](*args, **kwargs)


def chunk(name):
    G['CHUNK'] = name


def box(*args, **kwargs): return call('box', *args, **kwargs)
def mesh(*args, **kwargs): return call('mesh', *args, **kwargs)
def rod(*args, **kwargs): return call('cylinder', *args, **kwargs)
def oval(*args, **kwargs): return call('ellipsoid', *args, **kwargs)
def stroke(*args, **kwargs): return call('stroke', *args, **kwargs)
def floor(x, z): return call('height', x, z)


def section(x, z):
    # Spatial batches keep the new detail cheap without one enormous town mesh.
    return 'Detail_%d_%d' % (math.floor((x+175)/55), math.floor((z+76)/55))


def roof_kind(name):
    key = name.replace('COL_Roof_', '').replace(' pitched roof', '')
    if any(s in key for s in ('dormer','gable','Canvas','Tower','shed','Bakery','Tombo')): return 0
    return sum(ord(c) for c in key) % 3


def roof_form(name, x, y, z, w, d, rise, tile, parent=None):
    kind = roof_kind(name)
    if kind == 0:
        if name.startswith('COL_') or not any(s in name for s in ['pitched roof','dormer','gable roof']):
            return BASE_ROOF(name,x,y,z,w,d,rise,tile,parent)
        # The roof planes overhang plaster gable walls. Tiling a closed triangular
        # end cap made the old houses look like solid wedges of roofing material.
        vertices=[(x-w/2,y,z-d/2),(x+w/2,y,z-d/2),(x+w/2,y,z+d/2),(x-w/2,y,z+d/2),(x,y+rise,z-d/2),(x,y+rise,z+d/2)]
        roof=call('face_up',mesh(name,vertices,[(0,4,5,3),(1,2,5,4)],tile,parent))
        half=max(.3,w/2-.55);edge=rise*(1-half/(w/2))
        for side in [-1,1]:
            zz=z+side*(d/2-.60)
            mesh(name+' plaster gable',[(x-half,y,zz),(x+half,y,zz),(x+half,y+edge,zz),(x,y+rise,zz),(x-half,y+edge,zz)],[(0,1,2,3,4),(4,3,2,1,0)],FACADE_MATERIAL,parent)
        return roof
    # Hipped roofs and a steep lower mansard create distinct, grounded silhouettes.
    lower=[(x-w/2,y,z-d/2),(x+w/2,y,z-d/2),(x+w/2,y,z+d/2),(x-w/2,y,z+d/2)]
    if kind == 1:
        vertices=lower+[(x,y+rise,z-d*.29),(x,y+rise,z+d*.29)]
        faces=[(0,1,4),(1,2,5,4),(2,3,5),(3,0,4,5),(0,3,2,1)]
    else:
        inset=min(1.35,w*.19); top=y+rise*.77
        vertices=lower+[(x-w/2+inset,top,z-d/2+inset),(x+w/2-inset,top,z-d/2+inset),(x+w/2-inset,top,z+d/2-inset),(x-w/2+inset,top,z+d/2-inset),(x,y+rise,z-d*.27),(x,y+rise,z+d*.27)]
        faces=[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,8),(5,6,9,8),(6,7,9),(7,4,8,9),(0,3,2,1)]
    return mesh(name,vertices,faces,tile,parent)


def arch(name,x,y,z,w,h,material='White',front=-1):
    # Masonry voussoirs drawn as a single low-poly arch, above a recessed opening.
    points=[]
    for i in range(17):
        a=math.pi*i/16
        points.append((x+math.cos(a)*w/2,y+h+math.sin(a)*w*.25,z))
    stroke(name+' arch',points,.10,material)
    for dx in [-w/2,w/2]:box(name+' pier',(x+dx,y+h/2,z),(.19,h,.22),material)


def balcony(name,x,y,z,w,front):
    box(name+' bracketed ledge',(x,y,z),(w,.18,1.12),'Paint_7')
    for dx in [-w*.36,w*.36]:
        rod(name+' stone console',(x+dx,y-.7,z-front*.45),(x+dx,y-.12,z+front*.38),.11,'Paint_7',vertices=6)
    rod(name+' balcony handrail',(x-w/2,y+.96,z+front*.48),(x+w/2,y+.96,z+front*.48),.045,'Paint_6',vertices=6)
    for i in range(int(w/.36)+1):
        dx=-w/2+i*w/int(w/.36)
        rod(name+' balcony iron',(x+dx,y+.15,z+front*.48),(x+dx,y+.94,z+front*.48),.026,'Paint_6',vertices=5)
    planter(name+' geranium box',x,y+.22,z+front*.53,w*.72)


def planter(name,x,y,z,w=2):
    box(name+' trough',(x,y,z),(w,.30,.42),'Paint_6')
    for i in range(max(3,int(w*2))):
        px=x-w*.42+i*w*.84/(max(3,int(w*2))-1)
        oval(name+' leaves',(px,y+.23,z),(.36,.23,.30),'Leaf',segments=7,rings=5)
        call('flower_head',name+' bloom',px,y+.47,z,'Flower')


def frontage(name,x,z,w,d,h,paint=0,style='shop',front=-1):
    global FACADE_MATERIAL
    # Preserve hero locations. Ordinary original lots become attached 6–10 m homes.
    hero=name in ['Osono_Bakery','Tombo_Workshop','Madame_House','Harbor_Post_House','Orchard_Potting_Shed']
    count=1 if hero else max(2,round(w/8.0))
    if name=='Osono_Bakery':w=17;h=10.0;z=24;d=17
    if name=='Madame_House':w=20;h=11.1
    if name=='Harbor_Post_House':w=17;h=10.2
    rng=random.Random(name)
    for i in range(count):
        bw=w/count; bx=x-w/2+bw*(i+.5)
        bh=h if hero else min(11.8,max(8.4,h+rng.choice([-1.25,-.45,.7])))
        bd=d if hero else d+rng.choice([-.45,0,.45])
        # Street faces stay aligned; variation is taken into each rear yard.
        bz=z-front*(bd-d)/2
        bn=name if hero else name+'_Frontage_'+str(i+1)
        p=paint if hero else (paint+i)%3
        FACADE_MATERIAL='Paint_'+str(p)
        before=set(bpy.context.scene.objects)
        BASE_BUILDING(bn,bx,bz,bw-.08,bd,bh,p,style,front)
        parts=set(bpy.context.scene.objects)-before
        # Vergetimbers from the gable helper must not float above hip/mansard roofs.
        if roof_kind(bn)!=0:
            for obj in list(parts):
                if 'drawn roof verge' in obj.name or 'crooked ridge' in obj.name:
                    parts.remove(obj);bpy.data.objects.remove(obj,do_unlink=True)
        batch='Street_'+str(math.floor((bx+175)/50))+'_'+str(math.floor((z+76)/50))
        for obj in parts:
            if obj.type=='MESH' and not obj.name.startswith('COL_'):obj['chunk']=batch
        chunk(batch)
        y=floor(bx,bz);fz=bz+front*(bd/2+.27)
        for dx in [-bw/2+.18,bw/2-.18]:
            box(bn+' corner pilaster',(bx+dx,y+bh/2,fz),(.25,bh,.22),'Paint_7')
            for level in [1.05,3.45,6.85]:
                box(bn+' corner cap',(bx+dx,y+level,fz),(.42,.2,.31),'White')
        for level in [3.35,6.75]:
            if level<bh:box(bn+' string course',(bx,y+level,fz),(bw,.17,.24),'Paint_7')
        if not hero and (i+sum(map(ord,name)))%3==0:
            balcony(bn,bx-bw*.23,y+3.90,fz+front*.58,min(3.25,bw*.47),front)
        if style=='harbor':
            arch(bn+' loading portal',bx,y+.2,fz+front*.04,2.4,2.6,'Paint_7',front)
            box(bn+' hoist beam',(bx,y+bh-.55,fz+front*.85),(.22,.25,2.5),'Paint_6')
            rod(bn+' hoist rope',(bx,y+bh-.7,fz+front*1.8),(bx,y+3.7,fz+front*1.8),.023,'Paint_6',vertices=5)
        if not hero and style!='harbor':
            # A recessed attic window makes each roof read as an inhabited house.
            rz=bz+front*(bd/2+.07)
            if roof_kind(bn)!=0:
                # Hip/mansard faces slope away: give the opening a real dormer shell.
                center=bz+front*(bd/2-.65)
                box(bn+' small dormer',(bx,y+bh+.52,center),(1.5,1.20,2.2),'Paint_'+str(p))
                roof_form(bn+' small dormer cap',bx,y+bh+1.12,center,1.85,2.5,.68,'Paint_5' if style=='townhouse' else 'Paint_4')
                rz=bz+front*(bd/2+.49)
            call('window',bn+' attic',bx,y+bh+.62,rz,.70,.92,front,False)
        if hero and name=='Osono_Bakery':
            # Timber uprights, stone plinth, open shaded threshold and side garden.
            for dx in [-7.9,-4.6,-1.25,1.25,4.6,7.9]:
                box('Bakery shopfront upright',(x+dx,y+1.5,fz+front*.17),(.16,2.8,.26),'Paint_6')
            for dx in [-5.5,5.5]:planter('Bakery upper flowers',x+dx,y+5.0,fz+front*.34,3.0)
            call('window','Bakery front attic',x,y+bh+1.15,fz,1.6,1.5,front,True)
        # Rear service path connects each individual door to its shared block yard.
        rz=bz-front*(bd/2+1.3)
        box(bn+' rear service apron',(bx,y+.06,rz),(bw,.10,2.3),'Paint_7')


def leafy_mass(name,x,y,z,rx,ry,rz,seed,material='Paint_10'):
    obj=oval(name,(x,y,z),(rx,ry,rz),material,segments=11,rings=6)
    for v in obj.data.vertices:
        q=v.co
        # Scalloped clusters, flatter undersides and a broad asymmetric crown.
        angle=math.atan2(q.y,q.x)
        s=1+.12*math.sin(angle*5+seed)+.06*math.sin(angle*9-seed)
        q.x*=s;q.y*=s
        if q.z<-.15:q.z=-.15+(q.z+.15)*.48
        q.z+=.09*math.sin(q.x*4+q.y*3+seed)
    obj.data.update()
    return obj


def tree(name,x,z,size=1,kind='broad'):
    y=floor(x,z);seed=x*.78+z*.19
    # Keep roots and canopy in the same spatial batch as nearby planting.
    chunk(section(x,z))
    lean=.32*math.sin(seed)*size
    rod(name+' lower trunk',(x,y,z),(x+lean,y+3.4*size,z+.15*size),.29*size,'Paint_6',vertices=7,radius2=.20*size)
    rod(name+' upper trunk',(x+lean,y+3.3*size,z+.15*size),(x+lean*.5,y+6.1*size,z-.2*size),.20*size,'Paint_6',vertices=7,radius2=.055*size)
    if kind=='cypress':
        for i in range(4):leafy_mass(name+' cypress',(x+.12*math.sin(i+seed)),y+(3.4+i*1.55)*size,z, (1.2-i*.2)*size,2.0*size,(1.1-i*.16)*size,seed+i)
        return
    # Broad plane/linden silhouette, with branches visible below a layered crown.
    for i,(dx,dz,dy,s) in enumerate([(-2.1,-.7,5.8,1.0),(1.7,.4,6.6,1.1),(.0,-1.8,7.5,.98),(.3,1.7,6.1,.88),(-.4,.1,8.05,.92)]):
        rod(name+' branch',(x+lean,y+3.3*size,z),(x+dx*size,y+(dy-.5)*size,z+dz*size),.115*size,'Paint_6',vertices=6,radius2=.033*size)
        leafy_mass(name+' painted canopy',x+dx*size,y+dy*size,z+dz*size,2.7*size*s,1.55*size*s,2.55*size*s,seed+i)
    # A few loose branch-tip masses break the hard balloon edge at flight height.
    for i in range(4):
        a=seed+i*math.tau/4
        leafy_mass(name+' crown edge',x+math.cos(a)*3.5*size,y+(6.8+.5*math.sin(a))*size,z+math.sin(a)*3.4*size,.95*size,.66*size,1.0*size,seed+i)


def shrub(name,x,z,size=1,y=None,flower=None):
    chunk(section(x,z));y=floor(x,z) if y is None else y
    seed=x*.3-z*.45
    leafy_mass(name,x,y+.42*size,z,1.18*size,.84*size,.97*size,seed)
    if flower:
        for j in range(4):
            a=j*2.4+seed
            call('flower_head',name+' flower',x+math.cos(a)*.67*size,y+(1.22+.13*math.sin(a))*size,z+math.sin(a)*.57*size,flower)


def border(name,points,spacing=1.85,size=.7,flower=None):
    for a,b in zip(points,points[1:]):
        length=math.dist(a,b);n=max(1,math.ceil(length/spacing))
        for i in range(n):
            t=(i+.3)/n;x=a[0]+(b[0]-a[0])*t;z=a[1]+(b[1]-a[1])*t
            shrub(name,x+.17*math.sin(i*2.3),z+.17*math.cos(i*3.1),size*(.85+.18*math.sin(i*.8)),flower=flower)


def wild_margin(name,a,b,width=2.3):
    # A strip attached to a fence or path; never unbounded random scattering.
    rng=random.Random(name);length=math.dist(a,b);dx=(b[0]-a[0])/length;dz=(b[1]-a[1])/length
    for i in range(int(length*1.7)):
        t=rng.random();across=rng.uniform(-width/2,width/2)
        x=a[0]+(b[0]-a[0])*t-dz*across;z=a[1]+(b[1]-a[1])*t+dx*across
        y=floor(x,z)+.04;chunk(section(x,z));verts=[];faces=[]
        for j in range(4):
            angle=j*2.4+i;h=rng.uniform(.23,.58);vx=math.cos(angle);vz=math.sin(angle)
            k=len(verts);verts.extend([(x-vx*.12,y,z-vz*.12),(x+vx*.12,y,z+vz*.12),(x+vz*.16,y+h*.64,z-vx*.16),(x+vz*.26,y+h,z-vx*.26)])
            faces.extend([(k,k+1,k+2),(k,k+2,k+3),(k+2,k+1,k),(k+3,k+2,k)])
        mesh(name+' grasses',verts,faces,'LeafLight' if i%3 else 'Leaf')
        if i%5==0:call('flower_head',name+' meadow bloom',x,y+.4,z,'White' if i%2 else 'Gold')


def vine_support(name,x,y,z,w,h,front=-1):
    chunk(section(x,z))
    points=[(x+math.sin(i*.9)*w*.23,y+i*h/5,z+front*.17) for i in range(6)]
    stroke(name+' woody climbing stem',points,.032,'Paint_6')
    for i in [1,3,4]:
        vx,vy,vz=points[i]
        rod(name+' climbing branch',(vx,vy,vz),(vx+w*.22,vy+.4,z+front*.16),.021,'Paint_6',vertices=5)


def vine(name,x,y,z,w,h,front=-1):
    chunk(section(x,z))
    vine_support(name,x,y,z,w,h,front)
    for i in range(6):
        t=i/5;vx=x+math.sin(i*.9)*w*.23
        leafy_mass(name+' ivy',vx,y+t*h,z+front*.18,w*(.18+.06*math.sin(i)),.7,.20,i,'Paint_10')
        if i>2:call('flower_head',name+' rose',vx+.3,y+t*h+.25,z+front*.40,'Flower')


def barrel(name,x,y,z,size=.6):
    rod(name,(x,y,z),(x,y+size*1.5,z),size,'Paint_6',vertices=10,radius2=size*.92)
    for lift in [.2,1.22]:
        # Hoop mesh shares the wood batch's nearby region, rather than independent objects.
        points=[(x+math.cos(i*math.tau/12)*size*1.01,y+size*lift,z+math.sin(i*math.tau/12)*size*1.01) for i in range(13)]
        stroke(name+' iron hoop',points,.035,'Ink')


def crate(name,x,y,z,w=1.2):
    box(name,(x,y+w*.42,z),(w,w*.84,w*.75),'Paint_6')
    for side in [-1,1]:
        fz=z+side*w*.39
        for dy in [.08,.73]:box(name+' edge',(x,y+w*dy,fz),(w,.09,.06),'Paint_2')
        rod(name+' brace',(x-w*.45,y+.1,fz),(x+w*.45,y+w*.75,fz),.055,'Paint_2',vertices=4)


def wheel(name,x,y,z,r=.43):
    for offset in [-.04,.04]:
        points=[(x+offset,y+math.sin(j*math.tau/16)*r,z+math.cos(j*math.tau/16)*r) for j in range(17)]
        stroke(name+' rim',points,.035,'Paint_6')
    for j in range(6):
        a=j*math.pi/3;rod(name+' spoke',(x,y,z),(x,y+math.sin(a)*r,z+math.cos(a)*r),.019,'Paint_6',vertices=5)


def cart(name,x,z):
    y=floor(x,z);chunk(section(x,z))
    box(name+' bed',(x,y+.85,z),(1.6,.18,2.5),'Paint_6')
    for dx in [-.88,.88]:
        wheel(name,x+dx,y+.48,z)
        for dy in [1.1,1.4]:box(name+' side',(x+dx,y+dy,z),(.09,.2,2.55),'Paint_6')
        rod(name+' handles',(x+dx*.6,y+.8,z-1),(x+dx*.6,y+.65,z-2.6),.055,'Paint_6',vertices=6)
    for dx in [-.4,.4]:crate(name+' parcels',x+dx,y+.97,z,.7)


def clothesline(name,x,z):
    y=floor(x,z);chunk(section(x,z))
    for dx in [-3.5,3.5]:rod(name+' pole',(x+dx,y,z),(x+dx,y+3.5,z),.08,'Paint_6',vertices=6)
    stroke(name+' cord',[(x-3.5,y+3.4,z),(x,y+3.12,z),(x+3.5,y+3.4,z)],.016,'Paint_6')
    for i in range(5):
        px=x-2.55+i*1.18;lift=y+3.14+abs(i-2)*.065
        verts=[(px-.42,lift,z),(px+.42,lift,z),(px+.41,lift-1.45,z+.20),(px+.05,lift-1.55,z+.31),(px-.42,lift-1.42,z+.15)]
        mesh(name+' drying linen',verts,[(0,1,2,3,4),(4,3,2,1,0)],'White' if i%2 else 'Paint_3')


def greenhouse(x,z):
    chunk(section(x,z));y=floor(x,z);w=7;d=9
    box('Potting house stone base',(x,y+.25,z),(w,.5,d),'Paint_7')
    box('Greenhouse muted glass',(x,y+1.7,z),(w-.2,2.5,d-.2),'Glass')
    BASE_ROOF('Greenhouse glass roof',x,y+3,z,w,d,1.7,'Glass')
    for dx in [-w/2,0,w/2]:
        for dz in [-d/2,d/2]:rod('Greenhouse timber upright',(x+dx,y+.4,z+dz),(x+dx,y+3,z+dz),.07,'White',vertices=5)
    for j in range(7):
        zz=z-d/2+j*d/6
        stroke('Greenhouse glazing bar',[(x-w/2,y+3,zz),(x,y+4.7,zz),(x+w/2,y+3,zz)],.045,'White')
        for dx in [-w/2,w/2]:rod('Greenhouse window bar',(x+dx,y+.5,zz),(x+dx,y+3,zz),.045,'White',vertices=5)
    for lift in [1.7,3]:
        for dx in [-w/2,w/2]:box('Greenhouse transom',(x+dx,y+lift,z),(.08,.07,d),'White')
    c=box('COL_PottingGreenhouse',(x,y+1.7,z),(w,3.4,d),'Ink');c.hide_render=True
    G['BUILDINGS'].append(dict(name='Potting greenhouse',x=x,y=y,z=z,width=w,depth=d,height=4.7,style='garden'))


def finish_world():
    print('Environment detail: landmarks, gardens and working waterfront',flush=True)
    # Clock square: tall masonry shaft, recessed belfry, copper spire and corner buttresses.
    for obj in list(bpy.context.scene.objects):
        if obj.name.startswith(('Clock tower stone','Tower high slate roof','Tower cornice','COL_ClockTower')):
            bpy.data.objects.remove(obj,do_unlink=True)
    chunk('Clock_Square');x=23;z=36
    box('Tower masonry shaft',(x,12,z),(7.3,24,7.3),'Paint_7')
    box('Tower stepped base',(x,1.0,z),(9,2,9),'Paint_7')
    for lift in [6,17.7,24,28.2]:box('Clock tower stone belt',(x,lift,z),(8.1,.40,8.1),'White')
    for dx in [-3.55,3.55]:
        for dz in [-3.55,3.55]:box('Tower corner buttress',(x+dx,12,z+dz),(.65,24,.65),'Paint_7')
    box('Belfry shadow',(x,26.1,z),(7.0,4.1,7.0),'Glass')
    for dx in [-3.7,3.7]:
        for dz in [-3.7,3.7]:box('Belfry pillar',(x+dx,26.1,z+dz),(.5,4.1,.5),'Paint_7')
    for side in [-1,1]:
        for dx in [-1.85,1.85]:arch('Belfry twin arch',x+dx,24.3,z+side*3.7,2.5,2.2,'White',side)
    # Four-sided tapered copper roof, with lantern and needle against the sky.
    verts=[(x-4.5,28.5,z-4.5),(x+4.5,28.5,z-4.5),(x+4.5,28.5,z+4.5),(x-4.5,28.5,z+4.5),(x,34.5,z)]
    mesh('Clock copper spire',verts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],'Environment_Copper')
    rod('Tower finial',(x,34.4,z),(x,37,z),.09,'Gold',vertices=8,radius2=.015)
    oval('Tower finial orb',(x,35.2,z),(.25,.25,.25),'Gold',segments=10,rings=6)
    c=box('COL_ClockTower',(x,14.25,z),(9,28.5,9),'Ink');c.hide_render=True
    c=mesh('COL_ClockSpire',verts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(0,3,2,1)],'Ink');c.hide_render=True
    G['BUILDINGS'].append(dict(name='Clock tower',x=x,y=0,z=z,width=9,depth=9,height=37,style='landmark'))
    # A rear civic row frames the square without walling off its northern garden.
    frontage('Clock_Guildhall',43,47,16,10,10,0,'townhouse',-1)
    # Coherent terrace skirts at garden level, rather than raw floating lawn slabs.
    for side in [68,112]:
        for zz in [85,98,116]:
            chunk(section(side,zz));box('Garden wall coping',(side,8.28,zz),(.94,.22,11.2),'White')
    # Ground-floor vines grow from visible planted pockets along inhabited streets.
    for x,z,h,w,front in [(-121.3,15.2,7,3,-1),(-98,16.2,4.5,2,-1),(-144,-8.4,6,2,1),(33,-8.9,5.3,2.2,1),(106,-48.8,6,2,-1),(79,102.7,7,2,-1)]:
        y=floor(x,z);vine('Climbing street garden',x,y+.5,z,w,h,front)
        shrub('Vine root planting',x,z+front*.55,.6)
    # Bakery back yard: useful small objects share a service path and enclosure.
    call('path','Bakery service lane',[(-155,34),(-62,34)],2.4,'Paint_7')
    call('path','Bakery garden access',[(-132,10),(-132,34),(-132,48)],2.2,'Paint_7')
    for x in [-150,-78]:
        chunk(section(x,33));box('Shared rear garden wall',(x,floor(x,38)+.7,38),(16,1.4,.4),'Environment_Brick')
        box('Shared garden wall coping',(x,floor(x,38)+1.45,38),(16,.15,.54),'Paint_7')
        border('Warm garden border',[(x-7,39),(x+7,39)],1.7,.8,'Flower')
    clothesline('Bakery washing',-114,39);clothesline('Garden linen',-74,54)
    cart('Bakery delivery handcart',-126,18)
    chunk(section(-128,28));barrel('Bakery rain barrel',-128,floor(-128,28),28,.62)
    # Three shared service courts join the backs of the shop and harbor rows.
    for a,b in [((-151,-32),(-52,-32)),((-28,-32),(50,-32)),((76,-31),(150,-31))]:
        call('path','Shared block service lane',[a,b],3.6,'Paint_7')
        for x in [a[0]+3,b[0]-3]:
            shrub('Service court planting',x,a[1]+2.9,.8)
            chunk(section(x,a[1]));crate('Rear shop supplies',x,0,a[1]-2.8,.9)
    # Public park has masses, open grass rooms, a cross walk and clipped path edges.
    call('path','Park cross walk',[(-14,63),(52,63)],1.6,'Paint_7')
    border('Park southern lavender',[(-12,52),(11,52)],1.55,.65,'Lavender')
    border('Park southern roses',[(29,52),(51,52)],1.6,.7,'Flower')
    border('Park north hedge',[(-13,74),(13,74)],1.8,.85)
    border('Park north hedge',[(27,74),(52,74)],1.8,.85)
    for x,z in [(-8,59),(3,57),(35,70),(46,60)]:shrub('Park hydrangea',x,z,1.25,flower='Lavender')
    wild_margin('Park meadow flowers',(-11,66),(12,66),3)
    wild_margin('Park meadow flowers east',(28,58),(48,58),2)
    # Garden lanes and private parcels fill the formerly empty middle slopes.
    call('path','Courtyard lane west',[(-151,57),(-126,57),(-126,62),(-120,62)],2,'Paint_7')
    call('path','East garden walk',[(76,29),(145,29),(145,72),(76,72)],2.0,'Paint_7')
    for x,z in [(-143,47),(-109,50),(-85,43),(-58,48),(79,36),(96,47),(130,46),(142,62)]:
        border('Parcel hedge',[(x-5,z-4),(x-5,z+5),(x+5,z+5)],2.2,.85)
        wild_margin('Parcel flowers '+str(x),(x-4,z+3),(x+4,z+3),1.2)
    for x,z in [(91,31),(130,65),(-150,61),(-65,57)]:tree('Garden spreading plane',x,z,.85)
    greenhouse(124,39)
    call('path','Greenhouse access',[(124,29),(124,33)],1.8,'Paint_7')
    call('path','Allotment cross walk',[(76,51),(145,51)],1.8,'Paint_7')
    call('path','Allotment garden axis',[(116,29),(116,72)],1.8,'Paint_7')
    border('East garden enclosure',[(147,32),(147,69),(120,69)],1.65,1.03,'Lavender')
    call('bed','Pollinator garden',131,60,17,9,'Lavender')
    # Kitchen plots: soil, rows of vegetables, a water barrel, potting bench and trellis.
    for x,z in [(83,60),(100,60),(-144,64)]:
        chunk(section(x,z));box('Kitchen plot soil',(x,floor(x,z)+.10,z),(9,.13,5),'Paint_11')
        for row in range(3):
            for col in range(7):shrub('Kitchen cabbage',x-3.4+col*1.12,z-1.7+row*1.65,.24)
        chunk(section(x,z));barrel('Garden water butt',x+5,floor(x+5,z),z,.48)
        for dx in [-4,4]:rod('Pea trellis post',(x+dx,floor(x+dx,z+4),z+4),(x+dx,floor(x+dx,z+4)+2,z+4),.07,'Paint_6',vertices=6)
        for h in [.8,1.5]:rod('Pea trellis string',(x-4,floor(x-4,z+4)+h,z+4),(x+4,floor(x+4,z+4)+h,z+4),.023,'Paint_6',vertices=5)
        border('Pea vine row',[(x-4,z+4),(x+4,z+4)],1.3,.4)
    # Orchard floor: warm paths and wild margins keep tree trunks rooted in place.
    print('Environment detail: orchard and planted woodland edge',flush=True)
    for z in [109,144,157]:
        wild_margin('Orchard verge '+str(z),(-148,z+3),(-78,z+3),3)
        for x in [-138,-112,-86]:shrub('Orchard understory',x+3,z,.60)
    border('Pasture hedgerow',[(-157,74),(-157,98)],2.1,.9,'White')
    wild_margin('Pasture wildflowers',(-151,96),(-126,96),2.4)
    wild_margin('Pasture grasses',(-154,74),(-128,74),2)
    # Small potting shed terminates the orchard path below the tree line.
    frontage('Orchard_Potting_Shed',-143,174,8,6,4,2,'harbor',-1)
    call('path','Orchard north footpath',[(-140,136),(-140,169)],1.7,'Paint_7')
    for x,z in [(-159,162),(-95,173),(-53,168),(14,174),(48,167),(99,174),(148,165)]:
        border('Woodland edge',[(x-7,z-2),(x+8,z+2)],2.2,1.2)
        wild_margin('Woodland floor '+str(x),(x-8,z-4),(x+8,z-2),2.7)
    # A working waterfront, with paved aprons, piers, mooring lines and loading groups.
    print('Environment detail: quay and coast',flush=True)
    chunk('Quay_Details')
    box('Continuous waterfront promenade',(13,.055,-70),(282,.11,8),'Paint_7')
    box('Quay capstones',(10,.05,-75.6),(295,.24,1.3),'Paint_7')
    for x in [-118,-88,-8,47,92,145]:
        chunk(section(x,-70));barrel('Quay barrel',x,0,-71,.57);crate('Quay packing crate',x+1.6,0,-70.8)
        if x in [-88,47]:cart('Harbor loading cart',x,-69.5)
    for x,length in [(-58,19),(13,30),(69,16)]:
        chunk(section(x,-90))
        for j in range(int(length/.65)):
            box('Pier weathered boards',(x,-.4,-77-j*.65),(4.5,.22,.58),'Paint_6')
        for zz in [-78,-77-length]:
            for dx in [-2,2]:rod('Pier timber pile',(x+dx,-3,zz),(x+dx,.8,zz),.18,'Paint_6',vertices=8)
        stroke('Boat mooring rope',[(x-2,.55,-78),(x-3,-.4,-83),(x-3.7,-.55,-88)],.032,'Paint_6')
    # Net mending frame lives behind the boatyard rather than crossing the street.
    chunk(section(-58,-38))
    for x in [-61,-55]:rod('Net frame post',(x,0,-35),(x,3.6,-35),.10,'Paint_6',vertices=7)
    for i in range(10):
        x=-61+i*6/9;stroke('Hanging fishing net',[(x,3.4,-35),(x+.15,1.8,-34.8),(x,1,-35)],.018,'Paint_6')
    for i in range(6):rod('Fishing net weave',(-61,1+i*.48,-35),(-55,1+i*.48,-35),.018,'Paint_6',vertices=5)
    # Reeds and coastal rocks soften the artificial straight wall beyond the busy port.
    for side in [-1,1]:
        for i in range(10):
            x=side*(158+i*6);z=-77-(i%3)*1.7;chunk(section(x,z))
            oval('Coastal weathered rock',(x,-1.3,z),(3.0+(i%2),1.6,2.6),'Paint_7',segments=8,rings=5)
    harbor_boats()
    distant_views()
    # Route-scale source checks catch accidental static detail in a landing circle.
    validate_courts()


def hull(name, root, length, beam, depth, freeboard, topside, stripe, bottom):
    """A lofted clinker-style hull in the boat's own frame: bow toward +z, waterline at y = 0.

    Nine points per section (sheer, stripe, waterline, chine, keel and mirror) keep a painted
    topside, a boot stripe and a dark bottom as separate material bands.
    """
    sections = 14
    verts = []
    for i in range(sections + 1):
        t = i / sections
        half = beam / 2 * (.80 + .20 * math.sin(math.pi * min(1, t * 1.12))) * (1 - t ** 3.4) + .015
        sheer = freeboard * (1 + .42 * t ** 2.3 + .16 * (1 - t) ** 3)
        keel = -depth * (math.sin(math.pi * min(1, .06 + t * .98)) ** .45)
        z = (t - .5) * length
        side = [(half, sheer), (half * .99, freeboard * .28), (half * .95, 0), (half * .70, keel * .55), (0, keel)]
        ring = [(-x, y) for x, y in side] + [(x, y) for x, y in reversed(side[:-1])]
        verts.extend((x, y, z) for x, y in ring)
    n = 9
    faces, bands = [], []
    band = [0, 1, 2, 2, 2, 2, 1, 0]
    for i in range(sections):
        for j in range(n - 1):
            a = i * n + j
            faces.append((a, a + n, a + n + 1, a + 1))
            bands.append(band[j])
    faces.append(tuple(range(n - 1, -1, -1)))
    bands.append(0)
    data = bpy.data.meshes.new(name)
    data.from_pydata([G['xyz'](v) for v in verts], [], faces)
    data.update()
    # Orient every face away from the hull's centreline, without relying on a closed volume.
    for poly in data.polygons:
        centre = poly.center
        away = Vector((centre.x, 0, 0)) if abs(centre.x) > .05 else Vector((0, 0, -1 if centre.z < 0 else 1))
        if poly.index == len(faces) - 1:
            away = Vector((0, 1, 0))
        if poly.normal.dot(away) < 0:
            poly.flip()
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for material in (topside, stripe, bottom):
        obj.data.materials.append(G['MATERIALS'][material])
    for poly, b in zip(obj.data.polygons, bands):
        poly.material_index = b
    obj['chunk'] = 'Harbor_Boats'
    obj['separate'] = True
    obj.parent = root
    return obj


def boat_part(obj, root, heading=None):
    if obj is None:
        return None
    if heading is not None:
        # Boxes and ovals are authored axis-aligned; turn them with the hull.
        obj.rotation_euler[2] = math.radians(heading)
        bpy.context.view_layer.update()
    obj['separate'] = True
    obj.parent = root
    # Helper geometry is authored in world space; restate it in the boat's own frame.
    obj.matrix_parent_inverse = root.matrix_world.inverted()
    return obj


def harbor_boats():
    """Bespoke moored boats replace the three placeholder hulls: two sloops, a working
    fishing boat and two rowing boats at the piers. Each is a separate 'Boat_' assembly."""
    print('Environment detail: moored boats', flush=True)
    for obj in list(bpy.context.scene.objects):
        if obj.name.split('.')[0] in ('Boat hull', 'Boat deck', 'Mast', 'Sail'):
            bpy.data.objects.remove(obj, do_unlink=True)
    chunk('Harbor_Boats')
    boats = [
        ('sloop', -47, -96, 12, 'White', 'Bow', 'Paint_6'),
        ('sloop', 24, -118, 78, 'Paint_3', 'White', 'Paint_6'),
        ('fishing', 80, -93, -8, 'Paint_5', 'White', 'Bow'),
        ('rowing', -54.2, -88, 4, 'Paint_6', 'Paint_3', 'Paint_6'),
        ('rowing', 17.3, -101, -6, 'Paint_3', 'White', 'Paint_6'),
    ]
    for index, (kind, x, z, heading, topside, stripe, bottom) in enumerate(boats):
        root = G['empty']('Boat_%d' % index, (x, -2.02, z))
        root.rotation_euler = (0, 0, math.radians(heading))
        bpy.context.view_layer.update()
        m = root.matrix_world

        def world(p):
            # Boat frame (x starboard, y up, z bow) to the Unity-convention helpers' world space.
            v = m @ Vector(G['xyz'](p))
            return (v.x, v.z, -v.y)

        if kind == 'rowing':
            L, B = 4.2, 1.45
            hull('Rowing boat hull', root, L, B, .45, .38, topside, stripe, bottom)
            for zz in (-.9, .2, 1.1):
                boat_part(box('Rowing thwart', world((0, .30, zz)), (B * .82, .06, .26), 'Paint_6'), root, heading)
            for side in (-1, 1):
                boat_part(rod('Shipped oar', world((side * .38, .40, -1.6)), world((side * .30, .42, 1.3)), .035, 'Paint_6', vertices=6), root)
                boat_part(box('Oar blade', world((side * .30, .42, 1.45)), (.16, .03, .5), 'Paint_6'), root, heading)
            continue
        L, B = (9.5, 2.7) if kind == 'sloop' else (11.5, 3.4)
        hull('Moored hull', root, L, B, .95, .78 if kind == 'sloop' else .9, topside, stripe, bottom)
        deck_y = (.78 if kind == 'sloop' else .9) - .10
        boat_part(G['mesh']('Boat planked deck', [world((-B * .43, deck_y, -L * .47)), world((B * .43, deck_y, -L * .47)), world((B * .30, deck_y, L * .28)), world((0, deck_y + .1, L * .47)), world((-B * .30, deck_y, L * .28))], [(0, 1, 2, 3, 4), (4, 3, 2, 1, 0)], 'Paint_6'), root)
        if kind == 'sloop':
            mast = (0, deck_y, L * .12)
            top = (0, deck_y + 9.2, L * .12)
            boat_part(rod('Sloop mast', world(mast), world(top), .10, 'Paint_6', vertices=8, radius2=.07), root)
            boat_part(rod('Sloop boom', world((0, deck_y + 1.3, L * .12)), world((0, deck_y + 1.15, -L * .36)), .07, 'Paint_6', vertices=6), root)
            # Mainsail with a gentle belly, and a furled jib along the forestay.
            rows = 5
            pts = []
            for r in range(rows + 1):
                u = r / rows
                luff = (0, deck_y + 1.45 + u * 7.4, L * .12 - .08)
                leech = (0, deck_y + 1.3 + u * 7.5, L * .12 - (1 - u) * (L * .48) - .1)
                belly = math.sin(math.pi * .5) * .38 * (1 - u)
                mid = ((luff[0] + leech[0]) / 2 + belly, (luff[1] + leech[1]) / 2, (luff[2] + leech[2]) / 2)
                pts.append([world(luff), world(mid), world(leech)])
            verts = [p for row in pts for p in row]
            faces = []
            for r in range(rows):
                for c in range(2):
                    a = r * 3 + c
                    faces.append((a, a + 1, a + 4, a + 3))
                    faces.append((a + 3, a + 4, a + 1, a))
            boat_part(G['mesh']('Cream mainsail', verts, faces, 'White'), root)
            boat_part(rod('Forestay', world(top), world((0, deck_y + .5, L * .5)), .018, 'Ink', vertices=5), root)
            boat_part(rod('Backstay', world(top), world((0, deck_y + .5, -L * .48)), .018, 'Ink', vertices=5), root)
            boat_part(rod('Furled jib', world((0, deck_y + 7.4, L * .16)), world((0, deck_y + .8, L * .46)), .09, 'White', vertices=7), root)
        else:
            # Working boat: wheelhouse aft, a derrick mast and a heap of drying net.
            house = (0, deck_y + 1.1, -L * .22)
            boat_part(box('Wheelhouse walls', world(house), (B * .62, 2.2, 2.6), 'White'), root, heading)
            boat_part(box('Wheelhouse roof', world((0, deck_y + 2.28, -L * .22)), (B * .72, .16, 2.9), 'Paint_5'), root, heading)
            for side in (-1, 1):
                boat_part(box('Wheelhouse window', world((side * B * .315, deck_y + 1.55, -L * .22)), (.06, .55, 1.6), 'Glass'), root, heading)
            boat_part(box('Wheelhouse window', world((0, deck_y + 1.55, -L * .22 + 1.31)), (B * .44, .55, .06), 'Glass'), root, heading)
            boat_part(rod('Derrick mast', world((0, deck_y, L * .12)), world((0, deck_y + 6.8, L * .12)), .11, 'Paint_6', vertices=8), root)
            boat_part(rod('Derrick boom', world((0, deck_y + 1.4, L * .12)), world((0, deck_y + 4.6, L * .42)), .07, 'Paint_6', vertices=6), root)
            boat_part(oval('Drying net heap', world((0, deck_y + .25, L * .22)), (1.1, .38, .9), 'Paint_8', segments=10, rings=6), root, heading)
            for dz in (-.1, .25):
                boat_part(oval('Float buoy', world((B * .35, deck_y + .2, L * dz)), (.22, .22, .22), 'Bow', segments=8, rings=6), root)
        boat_part(G['stroke']('Mooring line', [world((0, .75, L * .5)), world((.4, .1, L * .5 + 1.4)), world((.9, -.15, L * .5 + 2.6))], .03, 'Paint_6'), root)


def distant_views():
    """Headlands close the bay, a lighthouse marks the eastern point, low islands sit in the
    haze and a hill town continues Koriko beyond the playable district. Fog paints these
    into the horizon; none of them are reachable or collide."""
    print('Environment detail: headlands, islands and distant town', flush=True)
    chunk('Distant_Coast')
    for side in (-1, 1):
        cx, cz = side * 455, -150
        # A main hill with seaward lobes: points and coves instead of one perfect ellipse.
        lobes = [(0, 0, 170, 26, 120), (-side * 95, -95, 70, 17, 52), (-side * 30, -128, 58, 13, 40), (side * 60, -112, 64, 15, 46)]
        for dx, dz, rx, h, rz in lobes:
            oval('Headland meadow', (cx + dx, -6, cz + dz), (rx, h, rz), 'Paint_9', segments=26, rings=12)
            oval('Headland limestone cliff', (cx + dx + side * 5, -10, cz + dz - 7), (rx * 1.05, h * .9, rz * 1.07), 'Paint_7', segments=26, rings=10)
        for i in range(5):
            a = i * 1.3 + (0 if side < 0 else .7)
            oval('Headland sea stack', (cx - side * 40 + math.cos(a) * 110, -3, cz - 150 + math.sin(a) * 18), (4 + i % 3, 5 + (i % 2) * 3, 4), 'Paint_7', segments=8, rings=6)
        for i in range(7):
            a = i * .9 + (0 if side < 0 else 2)
            oval('Headland pine grove', (cx + math.cos(a) * 70, 16 - (i % 3) * 2, cz + 30 + math.sin(a) * 45), (14, 7, 11), 'Paint_10', segments=10, rings=6)
    # The lighthouse stands on the eastern headland, looking out over the bay.
    lx, lz, ly = 402, -228, 10.8
    rod('Lighthouse tower', (lx, ly, lz), (lx, ly + 15, lz), 2.3, 'White', vertices=18, radius2=1.75)
    rod('Lighthouse band', (lx, ly + 6.2, lz), (lx, ly + 8.4, lz), 2.08, 'Bow', vertices=18, radius2=2.0)
    rod('Lighthouse gallery', (lx, ly + 15, lz), (lx, ly + 15.4, lz), 2.6, 'Ink', vertices=18)
    rod('Lighthouse lantern room', (lx, ly + 15.4, lz), (lx, ly + 17.6, lz), 1.5, 'Glass', vertices=14)
    rod('Lighthouse cap', (lx, ly + 17.6, lz), (lx, ly + 19.6, lz), 1.8, 'Bow', vertices=14, radius2=.12)
    box('Keeper cottage', (lx - 8, ly + 1.4, lz + 6), (7, 2.8, 5), 'Paint_0')
    BASE_ROOF('Keeper cottage roof', lx - 8, ly + 2.8, lz + 6, 7.6, 5.6, 1.6, 'Paint_4')
    G['empty']('LighthouseGlow', (lx, ly + 16.5, lz))
    chunk('Distant_Islands')
    for x, z, rx, rz, h in ((-215, -560, 95, 48, 20), (120, -640, 125, 58, 28), (365, -520, 72, 40, 15)):
        oval('Distant island', (x, -8, z), (rx, h + 8, rz), 'Distant', segments=22, rings=10)
    rng = random.Random('Koriko hill town')
    chunk('Distant_Town')
    placed = 0
    for i in range(46):
        x = 225 + rng.uniform(0, 170)
        z = 205 + rng.uniform(0, 170) + (x - 225) * .25
        y = call('height', x, z)
        w, d, h = rng.uniform(7, 12), rng.uniform(6, 9), rng.uniform(6, 11)
        box('Hill town house', (x, y + h / 2 - .5, z), (w, h, d), 'Paint_%d' % rng.choice((0, 0, 1, 2)))
        BASE_ROOF('Hill town roof', x, y + h - .5, z, w + .8, d + .8, h * .3, 'Paint_4' if rng.random() < .7 else 'Paint_5')
        placed += 1
    sx, sz = 310, 318
    sy = call('height', sx, sz)
    box('Hill town church nave', (sx, sy + 6, sz), (9, 12, 20), 'Paint_0')
    rod('Hill town church spire', (sx, sy + 12, sz - 8), (sx, sy + 34, sz - 8), 3.2, 'Environment_Copper', vertices=8, radius2=.2)
    box('Hill town church tower', (sx, sy + 9, sz - 8), (6, 18, 6), 'Paint_0')
    print('DISTANT_VIEWS: two headlands, a lighthouse, three islands and', placed, 'hill-town houses', flush=True)


def validate_courts():
    bpy.context.view_layer.update()
    for court in G['CatalogDestinations']:
        # Only explicitly physical structures; floor meshes and light props are excluded.
        for obj in bpy.context.scene.objects:
            if not obj.name.startswith('COL_') or obj.name in ['COL_Ground','COL_MadameGarden','COL_AirshipPlatform']:continue
            coords=[obj.matrix_world@Vector(v) for v in obj.bound_box]
            xmin=min(v.x for v in coords);xmax=max(v.x for v in coords)
            zmin=min(-v.y for v in coords);zmax=max(-v.y for v in coords)
            ymin=min(v.z for v in coords);ymax=max(v.z for v in coords)
            dx=max(xmin-court['x'],0,court['x']-xmax);dz=max(zmin-court['z'],0,court['z']-zmax)
            radius=2.5 if court['id']=='airship' else 3.0
            if dx*dx+dz*dz<radius*radius and ymax>court['y']+.35 and ymin<court['y']+3:
                raise RuntimeError('Landing clearance blocked: '+court['id']+' / '+obj.name)
    print('ENVIRONMENT_LAYOUT_PASS: six landing centers clear of building collision',flush=True)

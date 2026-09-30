"""Kiki model study, authored against the official film stills in Docs/CHARACTER.md.

All surfaces are original editable geometry. Coordinates are x/right, y/up,
z/forward; build_art supplies the Blender/Unity conversion and export helpers.
"""
import math
import bpy
from mathutils import Vector


def build_character(api):
    mesh, ellipsoid, empty, stroke = [api[k] for k in ('mesh', 'ellipsoid', 'empty', 'stroke')]
    xyz, materials = api['xyz'], api['MATERIALS']
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    api['CHUNK'] = 'Kiki'

    palette = {
        'Skin': (.99, .89, .81), 'SkinShade': (.91, .74, .69), 'Hair': (.32, .17, .13),
        'Dress': (.23, .19, .31), 'Bow': (.86, .08, .12),
        'BowShade': (.58, .04, .09), 'Ink': (.06, .045, .055),
        'Eye': (.06, .045, .055), 'Iris': (.17, .085, .065), 'White': (.99, .98, .94),
        'Shoe': (.66, .20, .14), 'Blush': (.98, .68, .70),
        'HairShade': (.14, .07, .06), 'DressShade': (.12, .10, .18),
        'Lip': (.55, .28, .26), 'Sole': (.19, .15, .15),
        'Satchel': (.94, .62, .36), 'Jiji': (.10, .105, .15), 'JijiInner': (.44, .33, .46),
    }
    for name, color in palette.items():
        if name not in materials:
            api['mat'](name, color)
        m = materials[name]
        linear=tuple(c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in color)
        m.diffuse_color = (*linear, 1)
        for node in m.node_tree.nodes:
            if node.type == 'MIX_RGB' and node.blend_type == 'MULTIPLY':
                node.inputs[1].default_value = (*linear, 1)
            if node.type == 'VALTORGB':
                node.color_ramp.interpolation = 'CONSTANT'
                node.color_ramp.elements[0].position = .12
                node.color_ramp.elements[0].color = (.64, .66, .75, 1)
                node.color_ramp.elements[1].position = .52
                node.color_ramp.elements[1].color = (1, 1, 1, 1)

    root = empty('KikiRig')
    for name, position in [('Origin', (0, 0, 0)), ('Right', (1, 0, 0)), ('Up', (0, 1, 0)), ('Forward', (0, 0, 1))]:
        empty('RiderAnchor_' + name, position, root)

    def attach(obj, parent):
        bpy.context.view_layer.update()
        matrix = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = matrix
        return obj

    def origin(obj, position):
        """Keep authored world geometry while placing a useful animation pivot."""
        offset = Vector(xyz(position))
        for v in obj.data.vertices:
            v.co -= offset
        obj.location = offset
        return obj

    def smooth(obj):
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
        return obj

    def part(name, pos, size, material, parent):
        return attach(ellipsoid(name, pos, size, material, segments=24, rings=16), parent)

    def soft_box(name, pos, size, material, parent, power=.35):
        # A superellipsoid: soft leather and cloth shapes with flat faces and rounded edges.
        rings, segments = 14, 28
        f = lambda c: math.copysign(abs(c)**power, c)
        vertices, faces = [], []
        for j in range(rings+1):
            v = -math.pi/2+math.pi*j/rings
            for i in range(segments):
                u = -math.pi+math.tau*i/segments
                vertices.append((pos[0]+size[0]*f(math.cos(v))*f(math.cos(u)), pos[1]+size[1]*f(math.sin(v)), pos[2]+size[2]*f(math.cos(v))*f(math.sin(u))))
        for j in range(rings):
            for i in range(segments):
                a=j*segments+i; b=j*segments+(i+1)%segments
                faces.append((a, b, b+segments, a+segments))
        return attach(smooth(mesh(name, vertices, faces, material)), parent)

    def line(name, points, width, material, parent):
        return attach(stroke(name, points, width, material), parent)

    def catmull(values, t):
        t = max(0, min(len(values) - 1, t))
        i = min(len(values) - 2, int(t)); f = t - i
        p0, p1, p2, p3 = [values[max(0, min(len(values) - 1, j))] for j in (i - 1, i, i + 1, i + 2)]
        return .5 * (2*p1 + (-p0+p2)*f + (2*p0-5*p1+4*p2-p3)*f*f + (-p0+3*p1-3*p2+p3)*f*f*f)

    def interpolate(rows, y):
        y = max(rows[0][0], min(rows[-1][0], y))
        i = next((j for j in range(len(rows)-1) if rows[j+1][0] >= y), len(rows)-2)
        f = (y-rows[i][0]) / max(.0001, rows[i+1][0]-rows[i][0])
        return tuple(catmull([row[k] for row in rows], i+f) for k in range(1, len(rows[0])))

    def bezier(points, t):
        a, b, c, d = [Vector(p) for p in points]
        return (1-t)**3*a + 3*(1-t)**2*t*b + 3*(1-t)*t*t*c + t**3*d

    def tube(name, centers, radii, material, parent, segments=24, steps=5, caps=True):
        """A continuously curved limb/cloth tube, with a stable cross-section."""
        vertices, faces = [], []
        count = (len(centers)-1)*steps+1
        for j in range(count):
            t = j/(count-1)*(len(centers)-1)
            center = Vector([catmull([p[k] for p in centers], t) for k in range(3)])
            ahead = Vector([catmull([p[k] for p in centers], min(len(centers)-1, t+.01)) for k in range(3)])
            behind = Vector([catmull([p[k] for p in centers], max(0, t-.01)) for k in range(3)])
            tangent = (ahead-behind).normalized()
            axis = Vector((1,0,0))
            if abs(tangent.dot(axis)) > .93: axis = Vector((0,0,1))
            u = (axis-tangent*axis.dot(tangent)).normalized(); v = tangent.cross(u).normalized()
            rx = max(.001, catmull([r[0] for r in radii], t))
            rz = max(.001, catmull([r[1] for r in radii], t))
            for k in range(segments):
                a = math.tau*k/segments
                p = center+u*math.cos(a)*rx+v*math.sin(a)*rz
                vertices.append(tuple(p))
        for j in range(count-1):
            for k in range(segments):
                a=j*segments+k; b=j*segments+(k+1)%segments
                faces.append((a,b,b+segments,a+segments))
        if caps:
            faces.append(tuple(reversed(range(segments))))
        if caps is True:
            faces.append(tuple(range((count-1)*segments,count*segments)))
        return attach(smooth(mesh(name, vertices, faces, material)), parent)

    body = empty('Body', (0,1.14,-.04), root)
    head = attach(empty('Head', (0,1.76,.02)), body)
    # The jaw, cheek and brow profiles describe an actual face rather than a sphere.
    face_rows = [
        (1.755,.026,.107,.025), (1.775,.083,.157,.071),
        (1.815,.159,.191,.133), (1.87,.218,.214,.185),
        (1.93,.249,.223,.214), (2.00,.257,.215,.230),
        (2.075,.255,.209,.235), (2.15,.240,.194,.222),
        (2.215,.197,.155,.181), (2.265,.116,.086,.111),
        (2.287,.002,.002,.002),
    ]

    def face_front(x,y):
        rx, front, back = interpolate(face_rows,y)
        z=front * max(0,1-(x/max(.002,rx))**2)**.41
        nose=.030*math.exp(-(x/.028)**2-((y-1.932)/.027)**2)
        bridge=.015*math.exp(-(x/.026)**2-((y-1.981)/.048)**2)
        return z+nose+bridge

    vertices,faces=[],[]
    n,levels=64,65
    for j in range(levels):
        y=face_rows[0][0]+(face_rows[-1][0]-face_rows[0][0])*j/(levels-1)
        rx,front,back=interpolate(face_rows,y)
        for i in range(n):
            a=math.tau*i/n; x=rx*math.cos(a); s=math.sin(a)
            z=face_front(x,y) if s>=0 else -back*abs(s)**.88
            vertices.append((x,y,z))
    for j in range(levels-1):
        for i in range(n):
            a=j*n+i; b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    faces += [tuple(reversed(range(n))),tuple(range((levels-1)*n,levels*n))]
    face=attach(smooth(mesh('Face',vertices,faces,'Skin')),head)
    # Broad cheek normals keep tiny topology changes out of the cel shadow boundary.
    face.data.normals_split_custom_set_from_vertices([
        Vector((v.co.x*.55, v.co.y, (v.co.z-2.01)*.45)).normalized() for v in face.data.vertices
    ])
    tube('Neck',[(0,1.60,-.018),(0,1.72,-.017),(0,1.81,.003)],[(.103,.092),(.077,.075),(.086,.084)],'SkinShade',body)

    def patch(name,cx,cy,rx,ry,material,parent,offset=.003,angle=0):
        """Thin painted feature fitted to the face: no protruding eyeball beads."""
        vertices=[]; faces=[]; slices=48; rings=5
        for j in range(rings+1):
            r=j/rings
            for i in range(slices):
                a=math.tau*i/slices
                u=rx*r*math.cos(a); v=ry*r*math.sin(a)
                x=cx+u*math.cos(angle)-v*math.sin(angle)
                y=cy+u*math.sin(angle)+v*math.cos(angle)
                vertices.append((x,y,face_front(x,y)+offset))
        for j in range(rings):
            for i in range(slices):
                a=j*slices+i;b=j*slices+(i+1)%slices;faces.append((a,b,b+slices,a+slices))
        obj=mesh(name,vertices,faces,material)
        # The open painted patch faces the front of the character.
        for polygon in obj.data.polygons:
            if polygon.normal.y>0: polygon.flip()
        obj=attach(origin(obj,(cx,cy,face_front(cx,cy))),parent)
        if name in ('Brown iris','Pupil','Eye glint','Lower glint'):
            obj.shape_key_add(name='Basis')
            for side,label in [(-1,'Gaze left'),(1,'Gaze right')]:
                key=obj.shape_key_add(name=label)
                for v in key.data:
                    v.co.x+=side*.008
                    # Paint must travel ON the face, not through the white patch.
                    # A straight local translation submerged one iris on turns.
                    v.co.y=-(face_front(cx+v.co.x,cy+v.co.z)+offset-face_front(cx,cy))
        return obj

    for side in (-1,1):
        label='Left' if side<0 else 'Right'
        eye_x=side*.102; eye_y=2.004
        eye=attach(empty(label+'EyePivot',(eye_x,eye_y,face_front(eye_x,eye_y))),head)
        patch('Eye white',eye_x,eye_y,.037,.048,'White',eye)
        patch('Brown iris',eye_x-side*.003,eye_y+.002,.030,.045,'Iris',eye,.005)
        patch('Pupil',eye_x-side*.003,eye_y+.004,.017,.032,'Eye',eye,.006)
        patch('Eye glint',eye_x-.010,eye_y+.019,.0085,.0105,'White',eye,.008)
        patch('Lower glint',eye_x+.010,eye_y-.019,.0042,.0050,'White',eye,.008)
        lid=[]
        for i in range(15):
            # Inner corner low, arching over the iris, ending in a heavier outer corner.
            t=i/14;a=math.pi*(1-t) if side<0 else math.pi*t
            x=eye_x+.041*math.cos(a);y=eye_y+.050*math.sin(a)**.85-.006*(1-math.sin(a))
            lid.append((x,y,face_front(x,y)+.006))
        line('Upper eyelid',lid,.0058,'Ink',eye)
        outer=eye_x+side*.036
        line('Upper lash flick',[(outer-side*.012,eye_y+.030,face_front(outer-side*.012,eye_y+.030)+.006),(outer,eye_y+.020,face_front(outer,eye_y+.020)+.006),(outer+side*.010,eye_y+.008,face_front(outer+side*.010,eye_y+.008)+.006)],.0068,'Ink',eye)
        line('Lower lid mark',[(eye_x+side*.006,eye_y-.047,face_front(eye_x+side*.006,eye_y-.047)+.005),(eye_x+side*.024,eye_y-.040,face_front(eye_x+side*.024,eye_y-.040)+.005)],.0026,'Lip',eye)
        brow=[]
        for i in range(9):
            t=i/8;x=eye_x-side*.030+side*t*.064;y=2.098+.008*math.sin(math.pi*t)
            brow.append((x,y,face_front(x,y)+.004))
        brow_pivot=attach(empty(label+'Brow',(eye_x,2.098,face_front(eye_x,2.098))),head)
        line(label+' eyebrow',brow,.0030,'HairShade',brow_pivot)
        patch('Cheek blush',side*.168,1.942,.034,.016,'Blush',head,.003)
        ear=part(label+' ear',(side*.264,1.971,.022),(.044,.072,.027),'Skin',head)
        line('Ear inner line',[(side*.277,2.003,.046),(side*.291,2.015,.043),(side*.288,1.978,.048)],.0028,'Lip',head)
    line('Nose mark',[(.011,1.922,face_front(.011,1.922)+.004),(.018,1.921,face_front(.018,1.921)+.004)],.0025,'Lip',head)
    line('Quiet smile',[(-.026,1.857,face_front(-.026,1.857)+.004),(-.009,1.853,face_front(-.009,1.853)+.004),(.012,1.853,face_front(.012,1.853)+.004),(.028,1.860,face_front(.028,1.860)+.004)],.003,'Lip',head)
    breath_mouth=patch('Breath mouth',0,1.855,.016,.022,'Lip',head,.005)

    # One continuous bob and fringe. The authored hairline follows the film's
    # irregular locks, so no separate slab roots can expose a shaved temple.
    hair_rows=[(1.76,.245,.219),(1.84,.293,.250),(1.96,.307,.268),(2.08,.301,.266),(2.18,.274,.244),(2.26,.206,.181),(2.305,.110,.099),(2.33,.004,.004)]
    hairline=[(-math.pi,1.80),(-2.12,1.81),(-1.77,1.88),(-1.50,1.998),(-1.22,2.060),(-1.05,2.118),(-.92,2.075),(-.80,2.083),(-.71,2.157),(-.46,2.067),(-.36,2.083),(-.28,2.163),(-.03,2.073),(.075,2.096),(.14,2.156),(.33,2.092),(.43,2.114),(.49,2.159),(.65,2.095),(.77,2.112),(.84,2.150),(1.02,2.065),(1.10,2.05),(1.22,2.066),(1.40,1.988),(1.77,1.864),(2.12,1.81),(math.pi,1.80)]
    vertices,faces=[],[];n=192;levels=36
    for j in range(levels):
        t=j/(levels-1)
        for i in range(n):
            a=math.tau*i/n
            angle=a if a<=math.pi else a-math.tau
            edge=interpolate(hairline,angle)[0]
            if abs(angle)<1.4:
                # Preserve the pointed, swept ends of the drawn fringe. A
                # smooth spline through every valley produces scalloped bangs.
                k=next(k for k in range(len(hairline)-1) if hairline[k+1][0]>=angle)
                a0,y0=hairline[k];a1,y1=hairline[k+1]
                f=(angle-a0)/(a1-a0)
                edge=y0+(y1-y0)*(f if y1>y0 else f**.7)
            if abs(angle)>1.8:
                tri=1-abs(2*((a*17/math.tau)%1)-1)
                edge+=.008*math.sin(a*19)-.034*tri**3*min(1,(abs(angle)-1.8)/.4)
            y=2.33+(edge-2.33)*t
            rx,rz=interpolate(hair_rows,y)
            tip=.009*math.sin(a*19)*max(0,(t-.83)/.17) if abs(angle)>1.8 else 0
            swept=a-.18*t*t*max(0,math.cos(a))
            x=(rx+tip)*math.sin(swept);z=-.034+(rz+tip)*math.cos(swept)
            vertices.append((x,y,z))
    for j in range(levels-1):
        for i in range(n):
            a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    hair=attach(smooth(mesh('Bob hair',vertices,faces,'Hair')),head)


    # Sparse swept part lines follow the same surface as the bob. The fringe is
    # the continuous edge of the cap, avoiding stacked slabs and exposed scalp.
    for side in (-1,1):
        points=[]
        for i in range(17):
            t=i/16;y=2.298-.132*t;x=.035+side*(.013+.115*t*t)-.040*t
            rx,rz=interpolate(hair_rows,y)
            z=-.034+rz*max(0,1-(x/max(.002,rx))**2)**.5+.004
            points.append((x,y,z))
        line('Hair part line',points,.0018,'HairShade',head)

    # Small tapered nape locks break the helmet silhouette at the back and sides.
    for side in (-1,1):
        for index in range(3):
            a=side*(1.93+index*.37)
            x=.287*math.sin(a);z=-.034+.25*math.cos(a)
            tube('Nape hair lock',[(x*.88,1.99,z*.90),(x,1.86,z),(x*1.07,1.79+.014*index,z-.013)],[(.034,.020),(.028,.014),(.001,.002)],'Hair',head,segments=12,steps=7)

    # The ribbon wraps around the bob; the bow is a pair of inflated cloth loops.
    vertices,faces=[],[]
    for i in range(65):
        a=-1.97+3.94*i/64
        x=.318*math.sin(a);y=2.035+.302*math.cos(a)
        for z in (-.082,-.028):vertices.append((x,y,z))
    for i in range(64):faces.append((i*2,i*2+1,i*2+3,i*2+2))
    band=attach(smooth(mesh('Red hair ribbon',vertices,faces,'Bow')),head)
    solid=band.modifiers.new('Ribbon thickness','SOLIDIFY');solid.thickness=.008
    bow=attach(empty('Bow',(0,2.333,-.048)),head)
    part('Bow knot',(0,2.338,-.015),(.047,.050,.043),'Bow',bow)
    for side in (-1,1):
        loop=attach(empty(('Left' if side<0 else 'Right')+'BowLoop',(side*.028,2.337,-.015)),bow)
        boundaries=[
            [(.025,2.337,0),(.073,2.420,0),(.179,2.601,0),(.254,2.597,0)],
            [(.254,2.597,0),(.335,2.602,0),(.410,2.487,0),(.380,2.415,0)],
            [(.380,2.415,0),(.346,2.335,0),(.115,2.306,0),(.025,2.337,0)],
        ]
        boundary=[]
        for controls in boundaries:
            boundary += [bezier(controls,i/20) for i in range(20)]
        center=Vector((.209,2.437,0));vertices=[];faces=[];n=len(boundary);rings=10
        for back in (0,1):
            for j in range(rings+1):
                r=j/rings
                for i,p in enumerate(boundary):
                    q=center+(p-center)*r
                    y=q.y+(.014 if side<0 else 0)*(q.x/.38)
                    bulge=.089*math.sqrt(max(0,1-r*r))
                    z=-.023+(.058 if side<0 else -.058)*q.x/.38 + (bulge if back==0 else -bulge*.85)
                    # A shallow crease leading away from the gathered knot.
                    z-=.012*math.exp(-((q.y-(2.331+q.x*.34))/.016)**2)*(1-r)
                    vertices.append((side*q.x,y,z))
        layer=(rings+1)*n
        for back in (0,1):
            for j in range(rings):
                for i in range(n):
                    a=back*layer+j*n+i;b=back*layer+j*n+(i+1)%n
                    faces.append((a,b,b+n,a+n))
        for i in range(n):
            a=rings*n+i;b=rings*n+(i+1)%n;faces.append((a,b,b+layer,a+layer))
        attach(smooth(mesh('Bow loop '+str(side),vertices,faces,'Bow')),loop)
        line('Bow gathered fold',[(side*.048,2.346,.004),(side*.116,2.369,.030),(side*.222,2.408,.044)],.0035,'BowShade',loop)
    bow.scale*=.80

    # Loose smock: broad continuous shoulders, no fitted waist or spherical cuffs.
    dress_rows=[(.50,.300,.262,-.020),(.53,.318,.272,-.022),(.70,.307,.260,-.022),(.93,.290,.242,-.020),(1.13,.274,.224,-.014),(1.36,.257,.198,.0),(1.50,.270,.178,.004),(1.58,.266,.162,.003),(1.655,.192,.124,.0),(1.685,.112,.096,.0)]
    vertices,faces=[],[];n=64;levels=49
    for j in range(levels):
        y=dress_rows[0][0]+(dress_rows[-1][0]-dress_rows[0][0])*j/(levels-1)
        rx,rz,cz=interpolate(dress_rows,y)
        for i in range(n):
            a=math.tau*i/n
            fold=.008*math.sin(a*7+.4)*(1-(y-.50)/1.2)
            hem=.018*math.sin(a*3+.5)*max(0,1-(y-.50)/.17)
            vertices.append(((rx+fold)*math.cos(a),y+hem,cz+(rz+fold)*math.sin(a)))
    for j in range(levels-1):
        for i in range(n):
            a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    dress=attach(smooth(mesh('Dress',vertices,faces,'Dress')),body)
    # The inside of the smock is painted in its flat shadow colour, as a cel painter fills a
    # skirt's interior; seen under the hem in flight it reads as drawn shade, not a hollow tube.
    dress.data.materials.append(materials['DressShade'])
    solid=dress.modifiers.new('Cloth thickness','SOLIDIFY');solid.thickness=.012;solid.offset=-1;solid.material_offset=1
    bpy.context.view_layer.objects.active=dress
    bpy.ops.object.modifier_apply(modifier=solid.name)
    def flight_cloth(obj):
        # The dress and its drawn folds share one deformation. Nothing floats
        # away from the cloth when the hem moves over the seated thighs.
        obj.shape_key_add(name='Basis')
        flight=obj.shape_key_add(name='Flight cloth')
        def fold(y,z,weight):
            angle=math.radians(-64)*weight
            dy=y-1.015;dz=(z+.075)*(1-.20*weight)
            return 1.015+dy*math.cos(angle)-dz*math.sin(angle),-.075+dy*math.sin(angle)+dz*math.cos(angle)+.165*weight
        knee_line=.64
        for v in flight.data:
            z,y=-v.co.y,v.co.z
            weight=max(0,min(1,(1.35-y)/.615))
            weight=weight*weight*(3-2*weight)
            # Seated astride, the front of the smock lies over the thighs while the back
            # hangs down behind her over the broom, closing the view under the hem.
            back=max(0,min(1,(-.02-z)/.16));back=back*back*(3-2*back)
            lap=weight*(1-.78*back)
            if y>=knee_line:
                fy,fz=fold(y,z,lap)
            else:
                # Below the knees the hem hangs down again and trails a little behind.
                ky,kz=fold(knee_line,z,1-.78*back);drop=knee_line-y
                fy,fz=ky-drop*.72,kz-drop*.62*(1-back)-.12*weight*(z+.075)
            v.co.z=fy;v.co.y=-fz
            v.co.x*=1+.12*weight
            # A little ease around the knees lets the limbs trail the body
            # without breaking through the side of the moving smock.
            v.co.z+=.022*weight
        # Additive wind shapes are authored in the seated cloth's frame. The
        # ink surface and drawn fold meshes receive exactly the same offsets.
        for side,name in [(-1,'Hem left'),(1,'Hem right')]:
            key=obj.shape_key_add(name=name)
            for source,v in zip(obj.data.vertices,key.data):
                x,y=source.co.x,source.co.z
                w=max(0,min(1,(1.36-y)/.625))**2
                edge=.5+.5*max(-1,min(1,side*x/.32))
                v.co.x+=side*.032*w
                v.co.z+=(.035*edge+.015)*w
                v.co.y+=.020*w
        # Standing cloth follows alternating steps, without folding into the
        # riding seat. The painted folds and outline use these same drawings.
        for side,name in [(-1,'Walk left'),(1,'Walk right')]:
            key=obj.shape_key_add(name=name)
            for source,v in zip(obj.data.vertices,key.data):
                w=max(0,min(1,(1.35-source.co.z)/.615))**2
                edge=max(0,min(1,.5+side*source.co.x/.64))
                v.co.x+=side*.046*w
                v.co.y-=.078*w*edge
                v.co.z+=.026*w*edge
    flight_cloth(dress)
    def on_dress(a,y,out=.006):
        rx,rz,cz=interpolate(dress_rows,y)
        return ((rx+out)*math.cos(a),y,cz+(rz+out)*math.sin(a))
    for a,top,bottom in [(1.10,1.02,.56),(2.02,.98,.58),(.42,1.28,.78),(2.72,1.28,.80),(-1.45,.92,.56)]:
        points=[on_dress(a+.10*t*t*(1 if a<1.57 else -1),top+(bottom-top)*t) for t in (0,.35,.7,1)]
        fold=line('Dress fold',points,.0038,'DressShade',body)
        flight_cloth(fold)

    # Limbs have articulated elbows and knees. Grip targets live on the broom,
    # allowing the runtime to solve the arms while the torso leans independently.
    broom=empty('Broom',(0,1.0,-.15),root)
    attach(empty('CarryGrip',(.051,.993,-.74)),broom)
    attach(empty('BroomGroundTip',(0,.91,-1.86)),broom)
    def relaxed_hand(obj,centers,side,segments,steps):
        obj.shape_key_add(name='Basis')
        key=obj.shape_key_add(name='Left hand relaxed')
        direction=Vector((-side*.65,-.64,.25)).normalized()
        count=(len(centers)-1)*steps+1
        for j in range(count):
            t=j/(count-1)*(len(centers)-1)
            old=Vector([catmull([p[k] for p in centers],t) for k in range(3)])
            new=Vector(centers[0])+direction*(j/(count-1)*.080)
            delta=Vector(xyz(new-old))
            for k in range(segments):key.data[j*segments+k].co+=delta
    for side in (-1,1):
        label='Left' if side<0 else 'Right'
        shoulder=(side*.247,1.602,.017)
        elbow=(side*.353,1.278,.140)
        wrist=(side*.046,1.074,.55+(side+1)*.045)
        arm=attach(empty(label+'Arm',shoulder),body)
        forearm=attach(empty(label+'Forearm',elbow),arm)
        hand=attach(empty(label+'Hand',wrist),forearm)
        attach(empty(label+'Grip',wrist),broom)
        sleeve_centers=[(side*.080,1.545,.010),(side*.250,1.535,.030),(side*.337,1.416,.103),(side*.356,1.262,.158)]
        sleeve=tube(label+' sleeve',sleeve_centers,[(.100,.122),(.128,.138),(.108,.117),(.112,.119)],'Dress',arm,caps='start')
        modifier=sleeve.modifiers.new('Cuff thickness','SOLIDIFY');modifier.thickness=.012;modifier.offset=-1
        tube(label+' upper arm',[shoulder,(side*.310,1.45,.09),elbow],[(.030,.030),(.061,.058),(.054,.052)],'Dress',arm)
        part(label+' elbow',elbow,(.057,.057,.057),'Skin',forearm)
        tube(label+' forearm',[elbow,(side*.251,1.203,.267),(side*.12,1.107,.41),wrist],[(.055,.052),(.059,.052),(.038,.037),(.034,.033)],'Skin',forearm)
        part(label+' palm',(wrist[0],wrist[1]-.013,wrist[2]),(.045,.039,.048),'Skin',hand)
        for i in range(4):
            z=wrist[2]-.034+i*.021
            centers=[(side*.064,1.070,z),(side*.027,1.080,z+.001),(side*.006,1.049,z+.004),(side*.026,1.024,z+.004)]
            finger=tube(label+' curled finger',centers,[(.012,.012),(.013,.013),(.011,.012),(.008,.009)],'Skin',hand,segments=10,steps=4)
            if side<0:relaxed_hand(finger,centers,side,10,4)
        tube(label+' thumb',[(side*.079,1.063,wrist[2]-.036),(side*.068,1.043,wrist[2]-.065),(side*.033,1.051,wrist[2]-.055)],[(.016,.017),(.017,.016),(.010,.012)],'Skin',hand,segments=12,steps=5)
        hip=(side*.145,1.015,-.075);knee=(side*.185,.595,.055);ankle=(side*.192,.139,.025)
        leg=attach(empty(label+'Leg',hip),body)
        shin=attach(empty(label+'Knee',knee),leg)
        foot=attach(empty(label+'Foot',ankle),shin)
        # The upper thigh stays inside the dress in every pose. Omit that
        # hidden surface so a wind ripple cannot expose intersecting skin.
        tube(label+' thigh',[(side*.181,.770,.048),(side*.184,.685,.053),knee],[(.078,.079),(.075,.071),(.069,.066)],'Skin',leg)
        part(label+' kneecap',knee,(.072,.074,.072),'Skin',shin)
        tube(label+' calf',[knee,(side*.19,.461,.035),(side*.193,.245,.018),ankle],[(.068,.065),(.074,.066),(.045,.044),(.035,.037)],'Skin',shin)
        part(label+' foot',(side*.192,.114,.067),(.049,.052,.080),'Skin',foot)
        # Red flats have a dark sole and a low opening showing the instep.
        shoe=tube(label+' shoe',[(side*.192,.080,-.067),(side*.192,.080,-.043),(side*.192,.078,.05),(side*.192,.070,.176),(side*.192,.066,.225)],[(.004,.004),(.039,.041),(.062,.041),(.063,.033),(.003,.003)],'Shoe',foot,segments=24,steps=7)
        part(label+' shoe toe',(side*.192,.096,.168),(.062,.058,.067),'Shoe',foot)
        tube(label+' sole',[(side*.192,.049,-.046),(side*.192,.044,.050),(side*.192,.041,.175),(side*.192,.041,.213)],[(.041,.007),(.064,.007),(.064,.006),(.028,.005)],'Sole',foot,segments=24,steps=6)

    # A small salmon shoulder bag is part of the film's delivery silhouette.
    bag=attach(empty('Satchel',(-.333,1.00,-.035)),body)
    soft_box('Satchel body',(-.358,.992,-.034),(.180,.150,.070),'Satchel',bag,.42)
    soft_box('Satchel flap',(-.366,1.045,.036),(.184,.104,.020),'Satchel',bag,.30)
    part('Satchel button',(-.368,.968,.058),(.016,.016,.006),'HairShade',bag)
    for side,points in enumerate([
        [( .175,1.648,.079),(.102,1.538,.172),(-.11,1.29,.219),(-.295,1.082,.080)],
        [( .175,1.648,-.073),(.095,1.524,-.167),(-.10,1.275,-.223),(-.292,1.086,-.117)],
    ]):
        centers=[bezier(points,j/30) for j in range(31)];vertices=[];faces=[]
        for p in centers:
            for dx in (-.016,.016):
                x=p.x+dx;rx,rz,cz=interpolate(dress_rows,p.y)
                z=cz+(1 if side==0 else -1)*(rz*max(0,1-(x/rx)**2)**.5+.018)
                vertices.append((x,p.y,z))
        for j in range(30):faces.append((j*2,j*2+1,j*2+3,j*2+2))
        obj=attach(mesh('Satchel shoulder strap',vertices,faces,'Satchel'),body)
        mod=obj.modifiers.new('Strap thickness','SOLIDIFY');mod.thickness=.010
    strap_top=[]
    for i in range(17):
        a=math.pi*i/16
        for x in (.159,.191):
            z=.001+.118*math.cos(a);low,high=1.50,1.685
            for _ in range(20):
                y=(low+high)/2;rx,rz,cz=interpolate(dress_rows,y)
                if (x/rx)**2+((z-cz)/rz)**2<=1:low=y
                else:high=y
            strap_top.append((x,max(1.649,low+.006),z))
    obj=attach(mesh('Satchel strap over shoulder',strap_top,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(16)],'Satchel'),body)
    mod=obj.modifiers.new('Strap thickness','SOLIDIFY');mod.thickness=.010

    # Tapered wood and individually shaped straw preserve a readable flying prop.
    tube('Broom handle',[(0,.975,-1.07),(0,1.018,-.15),(0,1.063,.65),(.015,1.090,1.33)],[(.031,.031),(.028,.028),(.025,.025),(.023,.023)],'Paint_6',broom,segments=16,steps=8)
    tube('Bound broom straw',[(0,.971,-.86),(0,.94,-1.10),(0,.915,-1.48),(0,.91,-1.72)],[(.058,.06),(.104,.09),(.201,.144),(.214,.113)],'Paint_15',broom,segments=32,steps=7)
    for i in range(30):
        a=i*2.39996;r=.207*math.sqrt((i+.5)/30)
        line('Broom straw',[(math.cos(a)*.035,.968+math.sin(a)*.03,-.89),(math.cos(a)*r*.60,.936+math.sin(a)*r*.51,-1.30),(math.cos(a)*r,.91+math.sin(a)*r*.66,-1.76-.11*math.sin(i*1.7)**2)],.0055,'Gold',broom)
    for i in range(14):
        a=math.tau*i/14
        points=[(rx*math.cos(a),y+ry*math.sin(a),z) for z,y,rx,ry in [(-.92,.965,.071,.071),(-1.10,.940,.108,.094),(-1.47,.918,.204,.148),(-1.69,.911,.219,.122)]]
        line('Broom drawn straw',points,.0027,'Paint_6',broom)
    for z in (-.925,-.972):
        points=[(.065*math.cos(i*math.tau/32),.968+.066*math.sin(i*math.tau/32),z) for i in range(33)]
        line('Broom binding',points,.012,'BowShade',broom)

    # Jiji at the film's scale: about a third of Kiki's head, slim and upright,
    # with big white eyes, slit pupils, tall ears and a long thin tail.
    jiji=attach(empty('Jiji',(0,1.04,-.76)),broom)
    tube('Jiji body',[(0,1.036,-.792),(0,1.085,-.798),(0,1.150,-.786),(0,1.205,-.768)],[(.068,.066),(.060,.058),(.045,.044),(.034,.033)],'Jiji',jiji)
    part('Jiji haunch',(0,1.060,-.805),(.072,.042,.074),'Jiji',jiji)
    cat_head=attach(empty('JijiHead',(0,1.205,-.765)),jiji)
    part('Jiji head',(0,1.248,-.756),(.066,.059,.056),'Jiji',cat_head)
    part('Jiji muzzle',(0,1.230,-.712),(.030,.022,.018),'Jiji',cat_head)
    for side in (-1,1):
        ear=attach(empty(('Left' if side<0 else 'Right')+'JijiEar',(side*.036,1.290,-.758)),cat_head)
        attach(smooth(mesh('Jiji ear',[(side*.014,1.287,-.770),(side*.056,1.392,-.772),(side*.066,1.268,-.742),(side*.040,1.296,-.745)],[(0,1,3),(1,2,3),(2,0,3),(2,1,0)],'Jiji')),ear)
        attach(mesh('Jiji inner ear',[(side*.026,1.296,-.747),(side*.053,1.370,-.757),(side*.058,1.279,-.740)],[(0,1,2),(2,1,0)],'JijiInner'),ear)
        part('Jiji eye',(side*.028,1.254,-.708),(.024,.029,.006),'White',cat_head)
        part('Jiji pupil',(side*.027,1.254,-.703),(.0065,.021,.003),'Ink',cat_head)
        tube('Jiji front paw',[(side*.020,1.150,-.748),(side*.021,1.085,-.736),(side*.022,1.040,-.728)],[(.013,.012),(.011,.011),(.015,.013)],'Jiji',jiji,segments=12,steps=4)
    part('Jiji nose',(0,1.235,-.694),(.0075,.0055,.004),'Lip',cat_head)
    line('Jiji mouth',[(0,1.226,-.696),(-.008,1.222,-.697)],.0018,'Ink',cat_head)
    tail=attach(empty('JijiTail',(0,1.045,-.83)),jiji)
    line('Jiji curling tail',[(0,1.045,-.83),(.07,1.052,-.93),(.125,1.12,-.975),(.13,1.24,-.96),(.095,1.305,-.925),(.06,1.315,-.9)],.011,'Jiji',tail)
    part('Jiji tail tip',(.06,1.315,-.9),(.011,.011,.011),'Jiji',tail)

    # Keep the crown attached while the lower bob and nape follow the wind.
    # These are editable morphs, shared by the native silhouette renderer.
    for obj in list(bpy.context.scene.objects):
        if obj.type!='MESH' or not (obj.name=='Bob hair' or obj.name.startswith('Nape hair lock')):continue
        obj.shape_key_add(name='Basis')
        for side,name in [(0,'Hair stream'),(-1,'Hair left'),(1,'Hair right')]:
            key=obj.shape_key_add(name=name)
            for v in key.data:
                x,y,z=v.co.x,v.co.z,-v.co.y
                w=max(0,min(1,(2.13-y)/.33))**2
                if z>.10 and abs(x)<.23:w*=.12
                v.co.x+=side*.100*w
                v.co.y+=(.125 if side==0 else .025)*w
                v.co.z+=(.04 if side==0 else .01)*w

    bpy.context.view_layer.update()
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH':
            if obj.data.materials[0].name=='Hair':
                # One controlled normal field unifies overlapping locks into a cel.
                matrix=obj.matrix_world;normal_matrix=matrix.to_3x3().transposed()
                normals=[]
                for vertex in obj.data.vertices:
                    p=matrix@vertex.co
                    n=Vector((p.x/.30,(p.y-.034)/.27,(p.z-2.065)/.28)).normalized()
                    normals.append((normal_matrix@n).normalized())
                obj.data.normals_split_custom_set_from_vertices(normals)
            if not obj.data.uv_layers:api['paint_uv'](obj,.5)
            api['paint_tones'](obj,True)
            # Taper the ink at cloth joins. An expanded silhouette around each
            # overlapping shoulder surface otherwise makes black hatch marks.
            if obj.name.endswith(' sleeve') or obj.name=='Dress':
                colors=obj.data.color_attributes['Paint tones']
                for vertex in obj.data.vertices:
                    y=vertex.co.z
                    if obj.name.endswith(' sleeve'):
                        t=max(0,min(1,(1.56-y)/.15))
                        mask=t*t*(3-2*t)
                    else:
                        u=max(0,min(1,(abs(vertex.co.x)-.18)/.08))
                        v=max(0,min(1,(y-1.50)/.09))
                        mask=1-u*u*(3-2*u)*v*v*(3-2*v)
                    colors.data[vertex.index].color[3]*=mask
    # The film's proportions: about five heads tall, not an oversized doll head.
    head.scale*=.84
    api['export']('KikiAndJiji')
    breath_mouth.hide_render=True
    render_study(api,'kiki-model',(3,2.35,4),(0,1.34,-.04),3.1)
    render_study(api,'kiki-front',(0,1.65,6),(0,1.40,0),3.05)
    render_study(api,'kiki-profile',(6,1.85,0),(0,1.35,0),3.1)
    render_study(api,'kiki-rear',(-3,2.6,-4),(0,1.35,-.1),3.1)
    render_study(api,'kiki-portrait',(1.2,2.19,3),(0,2.09,.02),1.25)


def render_study(api,name,pos,target,scale):
    scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT'
    scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.world.use_nodes=True
    scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.79,.81,.80,1)
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
    bpy.ops.object.light_add(type='SUN',location=(0,0,8));sun=bpy.context.object
    sun.data.energy=2;sun.rotation_euler=(.5,-.7,-.55)
    bpy.ops.object.camera_add(location=api['xyz'](pos));camera=bpy.context.object
    direction=Vector(api['xyz'](target))-camera.location
    camera.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=scale;scene.camera=camera
    scene.render.filepath=str(api['PREVIEWS']/(name+'.png'))
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera,do_unlink=True);bpy.data.objects.remove(sun,do_unlink=True)

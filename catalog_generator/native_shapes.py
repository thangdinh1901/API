"""Native Plant 3D fitting geometry, reconstructed as primitives.py shapes.

Each class mirrors a native ``varmain`` shape function 1:1 (disassembled from the
Plant 3D 2026 Python 3.11 content, D:/07. Python/variants). Dimensions come from
CATA_NUI.xlsx; geometry is drawn with primitives.py so parts are self-contained
and deploy without the ARX-only varmain runtime.

Shape -> class map (Excel ShapeName -> native module -> class here):
    CPFWR, CPFWR_F_SF  varmain/flangesub/cpfwr*        -> NativeWeldNeckFlange
    CPFLR              varmain/flangesub/cpflr         -> NativeLapJointFlange
    CPFBR              varmain/flangesub/cpfbr         -> NativeBlindFlange
    CPP                varmain/pipesub/cpp             -> NativePipe
    CPJRC              varmain/reducersub/.../cpjrc    -> NativeReducerConc
    CPJRE              varmain/reducersub/.../cpjre    -> NativeReducerEcc
    CPMUW              varmain/miscellaneoussub/cpmuw  -> NativeStubEnd

Convention (matches native): fitting axis is +X after rotateY(90). West port at
x=0 normal -X; east port at x=L (or L-I) normal +X. Units mm.
"""

import primitives as prim


def _cyl(s, diameter, height, bore_r=None):
    """CYLINDER(R, H, O) then rotateY(90) → tube along +X. O = bore radius.

    Native primitiv.CYLINDER(R,H,O): O is the inner (bore) radius; wall = R - O.
    bore_r None → solid (O = R).
    """
    r = diameter / 2.0
    o = r if bore_r is None else bore_r
    wall = max(0.0, r - o)
    return prim.Cylinder(s, diameter=diameter, height=height, wall_thickness=wall).rotateY(90)


class NativeWeldNeckFlange(prim.ShapeObject):
    """CPFWR / CPFWR_F_SF — disc + tapered hub + bore. SO, SW, WN flanges.

    L=overall length, B=disc thickness, D1=disc OD, D2=bore, D3=hub OD,
    socket_depth (I)=east port setback (0 for weld-neck). Hub tapers D3->D2.
    """

    def __init__(self, s, L, B, D1, D2, D3, socket_depth=0.0, *, add_ports=True):
        # Native CPFWR (weld-neck): clamp L; derive/fix hub OD; CONE hub D3->D2.
        if L <= 0.0 or L < B:
            L = B
        if D3 == 0.0:
            D3 = (D1 - D2) / 2.0 + D2
        elif D3 < 0.0:
            D3 = D2

        bore_r = D2 / 2.0

        o0 = _cyl(s, diameter=D1, height=B, bore_r=bore_r)  # disc tube
        if L > B:
            if D3 == D2:
                hub = _cyl(s, diameter=D3, height=L - B, bore_r=bore_r).move(x=B)
            else:
                # Native else-branch: tapered cone hub R1=D3/2 -> R2=D2/2.
                hub = prim.Cone(
                    s, bottom_diameter=D3, top_diameter=D2, height=L - B
                ).rotateY(90).move(x=B)
            o0.combine([hub])
            bore = _cyl(s, diameter=D2, height=L - B, bore_r=None).move(x=B)
            o0.subtract([bore])

        self._L = L
        self._I = socket_depth
        super().__init__(o0.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(s, prim.Point3d(0.0, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(
            s, prim.Point3d(self._L - self._I, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0)
        )
        return self


class NativeSlipOnFlange(prim.ShapeObject):
    """CPFWR_F_SF — slip-on / socket-weld flange: disc + STRAIGHT cylinder hub + bore.

    Same params as weld-neck but the native CPFWR_F_SF hub is a constant-diameter
    CYLINDER(R=D3/2), NOT a cone. socket_depth (I) sets the east (SO) port setback.
    """

    def __init__(self, s, L, B, D1, D2, D3, socket_depth=0.0, *, add_ports=True):
        if L <= 0.0 or L < B:
            L = B
        if D3 == 0.0:
            D3 = (D1 - D2) / 2.0 + D2
        elif D3 < 0.0:
            D3 = D2

        bore_r = D2 / 2.0
        o0 = _cyl(s, diameter=D1, height=B, bore_r=bore_r)  # disc tube
        if L > B:
            hub = _cyl(s, diameter=D3, height=L - B, bore_r=bore_r).move(x=B)  # straight hub
            o0.combine([hub])
            bore = _cyl(s, diameter=D2, height=L - B, bore_r=None).move(x=B)
            o0.subtract([bore])

        self._L = L
        self._I = socket_depth
        super().__init__(o0.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(s, prim.Point3d(0.0, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(
            s, prim.Point3d(self._L - self._I, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0)
        )
        return self


class NativeBlindFlange(prim.ShapeObject):
    """CPFBR — solid disc, one face port. L=thickness, D=OD."""

    def __init__(self, s, L, D, *, add_ports=True):
        o0 = _cyl(s, diameter=D, height=L, bore_r=None)
        super().__init__(o0.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(s, prim.Point3d(0.0, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        return self


class NativeLapJointFlange(prim.ShapeObject):
    """CPFLR — plain tube disc (no hub). L=thickness, D1=OD, D2=bore."""

    def __init__(self, s, L, D1, D2, *, add_ports=True):
        o0 = _cyl(s, diameter=D1, height=L, bore_r=D2 / 2.0)
        self._L = L
        super().__init__(o0.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(s, prim.Point3d(0.0, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(s, prim.Point3d(self._L, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0))
        return self


class NativePipe(prim.ShapeObject):
    """CPP — straight tube. D=OD, L=length, wall from schedule (DI supplied)."""

    def __init__(self, s, D, L, DI=None, *, add_ports=True):
        if L <= 0.0:
            L = D * 3.0  # native defaultPipeLength fallback
        bore_r = None if DI is None else DI / 2.0
        o0 = _cyl(s, diameter=D, height=L, bore_r=bore_r)
        self._L = L
        super().__init__(o0.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(s, prim.Point3d(0.0, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(s, prim.Point3d(self._L, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0))
        return self


class NativeReducerConc(prim.ShapeObject):
    """CPJRC — concentric reducer cone. D1=large OD, D2=small OD, L=length.

    wall (each end) supplied → hollow; else solid. Native drills inner cone.
    """

    def __init__(self, s, D1, D2, L, wall1=0.0, wall2=0.0, *, add_ports=True):
        o0 = prim.Cone(s, bottom_diameter=D1, top_diameter=D2, height=L).rotateY(90)
        if wall1 > 0.0 and wall2 > 0.0:
            inner = prim.Cone(
                s, bottom_diameter=D1 - 2.0 * wall1, top_diameter=D2 - 2.0 * wall2, height=L
            ).rotateY(90)
            o0.subtract([inner])
        self._L = L
        super().__init__(o0.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(s, prim.Point3d(0.0, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(s, prim.Point3d(self._L, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0))
        return self


class NativeReducerEcc(NativeReducerConc):
    """CPJRE — eccentric reducer. E=offset; default E=(D1-D2)/2 (flat bottom)."""

    def __init__(self, s, D1, D2, L, E=0.0, wall1=0.0, wall2=0.0, *, add_ports=True):
        if E == 0.0:
            E = D1 / 2.0 - D2 / 2.0
        o0 = prim.Cone(
            s, bottom_diameter=D1, top_diameter=D2, height=L, eccentricity=E
        ).rotateY(90)
        if wall1 > 0.0 and wall2 > 0.0:
            inner = prim.Cone(
                s, bottom_diameter=D1 - 2.0 * wall1, top_diameter=D2 - 2.0 * wall2,
                height=L, eccentricity=E,
            ).rotateY(90)
            o0.subtract([inner])
        self._L = L
        self._E = E
        prim.ShapeObject.__init__(self, o0.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(s, prim.Point3d(0.0, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(
            s, prim.Point3d(self._L, 0.0, -self._E), prim.Point3d(1.0, 0.0, 0.0)
        )
        return self


class NativeStubEnd(prim.ShapeObject):
    """CPMUW — lap-joint stub end: lap disc (D1) + tapered/straight barrel (D2), bored.

    L=overall, B=lap thickness, D1=lap OD, D2=barrel OD, wall=barrel wall.
    Native: barrel tube R2 bore R3=(R2-wall) over L-B at x=B; lap disc R1 bore R3
    at x=0; unite; if wall>0 drill R3 through barrel.
    """

    def __init__(self, s, L, B, D1, D2, wall=0.0, *, add_ports=True):
        R2 = D2 / 2.0
        R3 = R2 - wall  # bore radius of the barrel/lap
        # Barrel: OD D2, bore R3, length L-B, seated at x=B.
        barrel = _cyl(s, diameter=D2, height=L - B, bore_r=R3).move(x=B)
        # Lap disc: OD D1, bore R3, thickness B at x=0.
        lap = _cyl(s, diameter=D1, height=B, bore_r=R3)
        lap.combine([barrel])
        if wall > 0.0:
            bore = _cyl(s, diameter=2.0 * R3, height=L - B, bore_r=None).move(x=B)
            lap.subtract([bore])
        self._L = L
        super().__init__(lap.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(s, prim.Point3d(0.0, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(s, prim.Point3d(self._L, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0))
        return self


def _leg(s, diameter, length, base, direction, bore_r=None):
    """Straight cylinder leg of given OD/length from `base` point along unit `direction`
    (in XY plane). Cylinder is built along +Z, rotated to +X, spun by heading, moved to base."""
    import math
    dx, dy = direction[0], direction[1]
    heading = math.degrees(math.atan2(dy, dx))
    leg = _cyl(s, diameter=diameter, height=length, bore_r=bore_r)  # tube along +X
    # _cyl already rotateY(90) → axis +X; spin about Z to face `direction`.
    leg.obj.rotateZ(heading)
    leg.obj.translate((base[0], base[1], base[2]))
    return leg


class NativeElbow(prim.ShapeObject):
    """CPB — butt-weld elbow (equal bore). D=OD, R=bend radius, A=angle, wall=pipe wall.

    Arc body only (L1=L2=0 for BW). Bore = inner arc subtract when wall>0.
    Ports at arc inlet/outlet faces (from the Elbow wrapper, native pointAt/directionAt).
    """

    def __init__(self, s, D, R, A=90.0, wall=0.0, *, add_ports=True):
        body = prim.Elbow(s, diameter=D, bend_radius=R, angle=A)
        self._inlet = body.arc_inlet_position()
        self._outlet = body.arc_outlet_position()
        self._outdir = body.arc_outlet_direction()
        if wall > 0.0:
            inner = prim.Elbow(s, diameter=D - 2.0 * wall, bend_radius=R, angle=A)
            body.subtract([inner])
        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        ix, iy, iz = self._inlet
        prim.set_port(s, prim.Point3d(ix, iy, iz), prim.Point3d(1.0, 0.0, 0.0))
        ox, oy, oz = self._outlet
        dx, dy, dz = self._outdir
        prim.set_port(s, prim.Point3d(ox, oy, oz), prim.Point3d(dx, dy, dz))
        return self


class NativeElbowSocket(prim.ShapeObject):
    """CPB_OFOF — socket-weld elbow. D=forging OD, R(≈D/2 tight bend), A=angle,
    L1/L2=leg length (center→socket face), I1/I2=socket depth, wall=pipe wall,
    socket_bore=counterbore dia (pipe OD + clearance).

    Arc body + straight legs; each leg end counter-bored (socket) to depth I so the
    pipe seats inside. Ports set back by I at the socket bottom.
    """

    def __init__(self, s, D, R, A=90.0, L1=0.0, I1=0.0, L2=0.0, I2=0.0,
                 wall=0.0, socket_bore=0.0, body_od=0.0, *, add_ports=True):
        bd = body_od if body_od > 0.0 else D   # body/arc/leg OD = pipe OD
        if R <= bd / 2.0:
            R = bd / 2.0 + 0.0001  # native clamp
        if L2 <= 0.0:
            L2 = L1
        if I2 <= 0.0:
            I2 = I1

        body = prim.Elbow(s, diameter=bd, bend_radius=R, angle=A)
        inlet = body.arc_inlet_position()
        outlet = body.arc_outlet_position()
        outdir = body.arc_outlet_direction()

        # Legs at pipe OD from each arc face.
        if L1 > 0.0:
            body.combine([_leg(s, bd, L1, (inlet[0], inlet[1], inlet[2]), (1.0, 0.0, 0.0))])
        if L2 > 0.0:
            body.combine([_leg(s, bd, L2, (outlet[0], outlet[1], outlet[2]), outdir)])

        # Port faces at leg ends.
        self._p1 = (inlet[0] + L1, inlet[1], inlet[2])
        self._p2 = (outlet[0] + outdir[0] * L2, outlet[1] + outdir[1] * L2, outlet[2] + outdir[2] * L2)
        self._d2 = outdir

        # Forging bosses (OD D, length I) at each socket end — the step (gờ).
        if D > bd:
            if I1 > 0.0:
                base1 = (self._p1[0] - I1, self._p1[1], self._p1[2])
                body.combine([_leg(s, D, I1, base1, (1.0, 0.0, 0.0))])
            if I2 > 0.0:
                base2 = (self._p2[0] - self._d2[0] * I2,
                         self._p2[1] - self._d2[1] * I2,
                         self._p2[2] - self._d2[2] * I2)
                body.combine([_leg(s, D, I2, base2, self._d2)])

        # Through bore (pipe ID) — inner arc + inner legs.
        if wall > 0.0:
            ib = bd - 2.0 * wall
            body.subtract([prim.Elbow(s, diameter=ib, bend_radius=R, angle=A)])
            if L1 > 0.0:
                body.subtract([_leg(s, ib, L1 + 0.1, (inlet[0], inlet[1], inlet[2]), (1.0, 0.0, 0.0))])
            if L2 > 0.0:
                body.subtract([_leg(s, ib, L2 + 0.1, (outlet[0], outlet[1], outlet[2]), outdir)])

        # Socket counterbore (dia socket_bore, depth I) inside each boss — pipe seats here.
        if socket_bore > 0.0:
            if I1 > 0.0:
                base1 = (self._p1[0] - I1, self._p1[1], self._p1[2])
                body.subtract([_leg(s, socket_bore, I1 + 0.1, base1, (1.0, 0.0, 0.0))])
            if I2 > 0.0:
                base2 = (self._p2[0] - self._d2[0] * I2,
                         self._p2[1] - self._d2[1] * I2,
                         self._p2[2] - self._d2[2] * I2)
                body.subtract([_leg(s, socket_bore, I2 + 0.1, base2, self._d2)])

        self._I1, self._I2 = I1, I2
        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        # SW port seats at socket bottom (set back by I from the leg face).
        p1 = (self._p1[0] - self._I1, self._p1[1], self._p1[2])
        prim.set_port(s, prim.Point3d(*p1), prim.Point3d(1.0, 0.0, 0.0))
        d = self._d2
        p2 = (self._p2[0] - d[0] * self._I2, self._p2[1] - d[1] * self._I2, self._p2[2] - d[2] * self._I2)
        prim.set_port(s, prim.Point3d(*p2), prim.Point3d(d[0], d[1], d[2]))
        return self


class NativeTee(prim.ShapeObject):
    """CPTS — butt-weld tee. D1=run OD, D3=branch OD, L1/L2=run half-lengths
    (center→end), L3=branch length (center→end), A=branch angle, wall=pipe wall.

    Run along X (-L1..+L2); branch along +Y (0..L3). Reducing when D3<D1.
    """

    def __init__(self, s, D1, D3, L1, L2, L3, A=90.0, wall=0.0, *, add_ports=True):
        import math
        if D3 <= 0.0:
            D3 = D1
        if L2 <= 0.0:
            L2 = L1
        if L3 <= 0.0:
            L3 = L1

        # Run: OD D1 cylinder from x=-L1 to x=+L2.
        run = _cyl(s, diameter=D1, height=L1 + L2, bore_r=None).move(x=-L1)
        # Branch: OD D3 cylinder up +Y from 0 to L3 (native rotateX(270)+rotateZ(A-90)).
        branch = prim.Cylinder(s, diameter=D3, height=L3)
        branch.obj.rotateX(-90)  # +Z -> +Y
        heading = A - 90.0
        if abs(heading) > 1e-6:
            branch.obj.rotateZ(heading)
        run.combine([branch])

        if wall > 0.0:
            rbore = _cyl(s, diameter=D1 - 2.0 * wall, height=L1 + L2 + 0.1, bore_r=None).move(x=-L1 - 0.05)
            run.subtract([rbore])
            bbore = prim.Cylinder(s, diameter=D3 - 2.0 * wall, height=L3 + 0.1)
            bbore.obj.rotateX(-90)
            if abs(heading) > 1e-6:
                bbore.obj.rotateZ(heading)
            run.subtract([bbore])

        self._L1, self._L2, self._L3, self._A = L1, L2, L3, A
        super().__init__(run.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        import math
        prim.set_port(s, prim.Point3d(-self._L1, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(s, prim.Point3d(self._L2, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0))
        rad = math.radians(self._A - 90.0)
        bx, by = -math.sin(rad) * 0.0 + math.cos(rad) * 0.0, self._L3  # branch tip along +Y then rotZ
        # branch tip = rotZ(A-90) applied to (0, L3, 0)
        tx = -math.sin(rad) * self._L3
        ty = math.cos(rad) * self._L3
        dx, dy = -math.sin(rad), math.cos(rad)
        prim.set_port(s, prim.Point3d(tx, ty, 0.0), prim.Point3d(dx, dy, 0.0))
        return self


class NativeTeeSocket(prim.ShapeObject):
    """CPTS_OFOFOF — socket-weld tee. Run + branch legs, each end counter-bored
    (socket depth I, dia socket_bore). Ports set back by I at socket bottom.
    """

    def __init__(self, s, D1, D3, L1, L2, L3, I1=0.0, I2=0.0, I3=0.0,
                 A=90.0, wall=0.0, socket_bore=0.0, body_od=0.0, body_od3=0.0,
                 *, add_ports=True):
        import math
        if D3 <= 0.0:
            D3 = D1
        if L2 <= 0.0:
            L2 = L1
        if L3 <= 0.0:
            L3 = L1
        bd = body_od if body_od > 0.0 else D1        # run body OD (pipe)
        bd3 = body_od3 if body_od3 > 0.0 else (bd if D3 == D1 else D3)  # branch body OD
        heading = A - 90.0
        rad = math.radians(heading)

        # Run + branch at pipe OD.
        run = _cyl(s, diameter=bd, height=L1 + L2, bore_r=None).move(x=-L1)
        branch = prim.Cylinder(s, diameter=bd3, height=L3)
        branch.obj.rotateX(-90)
        if abs(heading) > 1e-6:
            branch.obj.rotateZ(heading)
        run.combine([branch])

        def _branch_boss_or_bore(diam, height, ztop):
            c = prim.Cylinder(s, diameter=diam, height=height).move(z=ztop)
            c.obj.rotateX(-90)
            if abs(heading) > 1e-6:
                c.obj.rotateZ(heading)
            return c

        # Forging bosses (OD D1/D3, length I) at each socket end — the step.
        if I1 > 0.0 and D1 > bd:
            run.combine([_cyl(s, diameter=D1, height=I1, bore_r=None).move(x=-L1)])
        if I2 > 0.0 and D1 > bd:
            run.combine([_cyl(s, diameter=D1, height=I2, bore_r=None).move(x=L2 - I2)])
        if I3 > 0.0 and D3 > bd3:
            run.combine([_branch_boss_or_bore(D3, I3, L3 - I3)])

        # Through bore (pipe ID).
        if wall > 0.0:
            run.subtract([_cyl(s, diameter=bd - 2.0 * wall, height=L1 + L2 + 0.1, bore_r=None).move(x=-L1 - 0.05)])
            run.subtract([_branch_boss_or_bore(bd3 - 2.0 * wall, L3 + 0.1, -0.05)])

        # Socket counterbores (dia socket_bore, depth I) inside each boss.
        if socket_bore > 0.0:
            if I1 > 0.0:
                run.subtract([_cyl(s, diameter=socket_bore, height=I1 + 0.1, bore_r=None).move(x=-L1 - 0.05)])
            if I2 > 0.0:
                run.subtract([_cyl(s, diameter=socket_bore, height=I2 + 0.1, bore_r=None).move(x=L2 - I2 - 0.05)])
            if I3 > 0.0:
                run.subtract([_branch_boss_or_bore(socket_bore, I3 + 0.1, L3 - I3 - 0.05)])

        self._L1, self._L2, self._L3 = L1, L2, L3
        self._I1, self._I2, self._I3 = I1, I2, I3
        self._rad = rad
        super().__init__(run.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        import math
        prim.set_port(s, prim.Point3d(-self._L1 + self._I1, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0))
        prim.set_port(s, prim.Point3d(self._L2 - self._I2, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0))
        depth = self._L3 - self._I3
        tx = -math.sin(self._rad) * depth
        ty = math.cos(self._rad) * depth
        dx, dy = -math.sin(self._rad), math.cos(self._rad)
        prim.set_port(s, prim.Point3d(tx, ty, 0.0), prim.Point3d(dx, dy, 0.0))
        return self

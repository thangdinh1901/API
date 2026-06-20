"""ASME B16.9 Type A long-pattern lap-joint stub end — Pipedata Pro Sch-40."""

import stubend_geom


class STUBENDLJABWSCH40(stubend_geom.StubEndLjA):
    def __init__(self, s, size, *, add_ports=True):
        super().__init__(s, size, pattern="long", add_ports=add_ports)

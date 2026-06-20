# Lap-joint CL150 stud lengths (mm) — RF grip + 2× stub lap thickness; separate from WN/SO STUD_RF.
# Mirrors CatalogFlangeBoltingCatalog.TryGetLjCl150 in export.

from __future__ import annotations

import pipe_sizes
from STUD_BOLTS import bolting_data


def lj_stud_length_mm(dn: int, pressure_class: int = 150) -> dict:
    """Return bolt size, count, and overall length for lap-joint FF joints."""
    rf = bolting_data.lookup(pressure_class, dn=dn, face=bolting_data.FaceType.RF)
    stub = pipe_sizes.stubend_lj_a_dims_mm(dn, "long")
    lap_t = float(stub["T"])
    return {
        "bolt": rf["bolt"],
        "n": rf["n"],
        "L": int(round(rf["L"] + 2.0 * lap_t)),
        "nps": rf["nps"],
    }

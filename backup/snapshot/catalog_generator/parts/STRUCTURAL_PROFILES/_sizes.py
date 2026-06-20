from enum import IntEnum
from typing import Mapping, TypeVar



SizeEnumT = TypeVar("SizeEnumT", bound=IntEnum)

def resolve_size(
    size: int | SizeEnumT,
    size_enum_cls: type[SizeEnumT],
    dimensions: Mapping[int, Mapping[str, float]],
) -> SizeEnumT:
    """
    Validate and convert size to the appropriate Enum.

    Args:
        size: int or enum instance
        size_enum_cls: The Enum class to cast to (e.g., ProfileUPN.Size)
        dimensions: dictionary of valid sizes

    Returns:
        Enum instance
    """
    if isinstance(size, size_enum_cls):
        return size
    if isinstance(size, int):
        if size in dimensions:
            return size_enum_cls(size)
        raise ValueError(
            f"Invalid size value: {size}. Must be one of {list(dimensions.keys())}."
        )
    raise TypeError(
        f"size must be an instance of {size_enum_cls.__name__} or int, got {type(size)}"
    )

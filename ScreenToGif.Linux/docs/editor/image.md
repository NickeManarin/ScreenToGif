# Image

The Image tab can resize, crop, horizontally or vertically flip, and rotate
selected frames in either direction. These operations are staged through FFmpeg
and participate in the editor's undo/redo history without changing frame timing
or order. Bounded Border and drop-shadow effects use the same safe path.

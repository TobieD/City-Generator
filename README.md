# CityGenerator

For my City Generator project, I created a tool to be used in Unity that allows a user to quickly generate cities based on his preferences. The user can specify what kinds of buildings and props will be used to populate the city as well as define trees, details and textures to create a terrain.
The generator uses a Voronoi diagram to create the layout, by first generating a specified amount of points in a 2D plane. The Voronoi diagram is then generated and using this data of cells, roads are derived from it.
Once the roads are created, buildings are placed next to it and additional props and trees are placed inside of each cell. This creates a more populated feeling. All of the visual aspects of the city can be customized.

The generator uses a Voronoi diagram to create the layout, by first generating a specified amount of points in a 2D plane. 
The Voronoi diagram is then generated and using this data of cells, roads are derived from it.

Once the roads are created, buildings are placed next to it and additional props and trees are placed inside of each cell. This creates a more populated feeling. All of the visual aspects of the city can be customized.
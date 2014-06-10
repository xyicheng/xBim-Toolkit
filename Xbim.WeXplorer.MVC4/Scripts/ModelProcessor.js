define(['eventmanager', 'Geometry/gl-matrix.1.3.7', 'Geometry/Geometry2Mesher'], function (eventmanager, glmatrix, GeometryMesher) {
    var processor = function ()
    {
        this.bounds;
        this.transform;
        this.library = {
            shapes: [],
            materials: [],
            maps: [],
            maplist: []
        };
        this.dictionary = {
            maps: [],
            shapes: []
        };
    }
    processor.prototype.HandleBoundsInstances = function (data)
    {
        if (!data) return;

        for (var i = 0; i < data.instances.length; i++)
        {
            var geom = data.instances[i];
            geom.Transformation = this.CreateMatrix(geom.Transformation);
            var xform = mat4.create();
            //mat4.multiply(transform, placement, xform);



            var geopiece = {
                id: geom.InstanceLabel,
            //    layerid: geom.StyleLabel,
            //    mapid: geom.MapLabel ? geom.MapLabel:0,
            //    Positions: mesh.Positions,
            //    Normals: mesh.Normals,
            //    Indices: mesh.Indices,
                data: { prod: geom.IfcProductLabel }
            };

            eventmanager.FireEvent("Box", geom);
        }
    }
    processor.prototype.HandleContext = function (data) {
        if (!data) return;
        if (!data.MetreFactor) return;

        this.MetreFactor = data.MetreFactor;

        if (!data.Regions || !data.Regions.length) return;

        //get the most populated region
        var mostPopulation = data.Regions[0], mostPopValue = data.Regions[0].Population;
        for (var i = 0; i < data.Regions.length; i++)
        {
            if (Math.max(data.Regions[i].Population, mostPopValue) === data.Regions[i].Population)
            {
                mostPopulation = data.Regions[i];
                mostPopValue = mostPopulation.Population;
            }
        }

        this.bounds = {
            mix: mostPopulation.Centre.X - (mostPopulation.Size.X/2),
            miy: mostPopulation.Centre.Y - (mostPopulation.Size.Y/2),
            miz: mostPopulation.Centre.Z - (mostPopulation.Size.Z/2),
            max: mostPopulation.Centre.X + (mostPopulation.Size.X/2),
            may: mostPopulation.Centre.Y + (mostPopulation.Size.Y/2),
            maz: mostPopulation.Centre.Z + (mostPopulation.Size.Z/2)
        };

        this.transform = mat4.create();
        mat4.identity(this.transform);
        mat4.translate(this.transform, vec3.create([-mostPopulation.Centre.X, -mostPopulation.Centre.Y, -mostPopulation.Centre.Z]), this.transform);
        
        eventmanager.FireEvent("ModelBounds", this.transform);
    }
    processor.prototype.HandleLibraryStyles = function (data) {
        if (!data || !data.length) return;
        for (var i = 0; i < data.length; i++)
        {
            //if(data[i].Material.Alpha >= 0.9999)
                this.library.materials[data[i].Material.MaterialID] = data[i].Material;
        }
        this.library.materials["0"] = { MaterialID:0, Red: 1.0, Green: 0.0, Blue: 1.0, Alpha: 0.0 };
        eventmanager.FireEvent("Materials", this.library.materials);
    }
    processor.prototype.HandleLibraryShapes = function (data) {
        if (!data || !data.length) return;
        this.library.maplist = data;
        return this.library.maplist;
    }
    processor.prototype.HandleProductShapes = function (data) {
        if (!data || !data.length) return;
        var i, j, k, product, Map, Mapping, key, shapes;

        for (i = 0; i < data.length; i++) {
           product = data[i];
           this.library.shapes[product.ProductLabel] = product;

            //iterate over all the maps
            for (j = 0; j < product.MappedShapes.length; j++)
            {
                Map = product.MappedShapes[j];
                //for the maps, iterate over all the individual mappings
                for(k =0; k < Map.Items.length; k++)
                {
                    //add it to our map dictionary so we can get a list of products from the maps
                    Mapping = Map.Items[k];
                    var obj = {
                        prod: product.ProductLabel,
                        placement: product.Placement,
                        transform: Map.Transform,
                        geoid: Mapping
                    };
                    if (this.dictionary.maps[Mapping]) {
                        this.dictionary.maps[Mapping].push(obj);
                    } else {
                        this.dictionary.maps[Mapping] = [obj];
                    }
                }
            }
            for (j = 0; j < product.Shapes.length; j++) {
                this.dictionary.shapes[product.Shapes[j]] = product.ProductLabel;
            }
        }

        //turn a sparse array into an array of keys
        shapes = [];
        for (key in this.dictionary.shapes) {
            shapes.push(key);
        }
        return shapes;
    }
    processor.prototype.HandleMapGeometry = function (data) {
        if (!data || !data.length) return;
        for (var i = 0; i < data.length; i++)
        {
            //var xform = this.CreateMatrix(data[i].Transform);
            //var mesh = GeometryMesher(data[i].Mesh, xform);
            //this.library.maps[data[i].GeometryLabel] = mesh;

            var products = this.dictionary.maps[data[i].GeometryLabel];
            if (products && products.length) {
                for (var k = 0; k < products.length; k++) {
                    //this.ConstructGeometry(geom, mesh, products[k]);
                    //console.log(products[k]);
                    /*
                    var obj = {
                        prod: product.ProductLabel,
                        placement: product.Placement,
                        transform: Map.Transform,
                        geoid: Mapping,
                        mapid:
                    };
                    /**/
                    var product = products[k];
                    var pplace = product.placement === "I" ? mat4.identity() : JSON.parse("[" + product.placement.split(" ").join(",") + "]");
                    var transform = product.transform === "I" ? mat4.identity() : JSON.parse("[" + product.transform.split(" ").join(",") + "]");
                    var placement = mat4.create(pplace);
                    var xform = mat4.create();
                    mat4.multiply(transform, placement, xform);

                    var mesh = GeometryMesher(data[i].Mesh, xform);

                    this.ConstructGeometry({ GeometryLabel: product.geoid, StyleLabel: data[i].StyleLabel, MapLabel: data[i].GeometryLabel }, mesh, product.prod);
                }
            }
        }
    }
    processor.prototype.HandleGeometry = function (data) {
        if (!data || !data.length) return;
        for (var i = 0; i < data.length; i++) {
            var geom = data[i];
            //this.library.shapes[product.ProductLabel]
            var prodlabel = this.dictionary.shapes[geom.GeometryLabel];
            var product = this.library.shapes[prodlabel];

            var pplace = product.Placement === "I" ? mat4.identity():JSON.parse("[" + product.Placement.split(" ").join(",") + "]");

            var placement = mat4.create(pplace);
            var transform = this.CreateMatrix(geom.Transform);
            var xform = mat4.create();
            mat4.multiply(transform, placement, xform);

            var mesh = GeometryMesher(geom.Mesh, xform);
            this.ConstructGeometry(geom, mesh, prodlabel);
        }
    }

    processor.prototype.ConstructGeometry = function (geom, mesh, label, transform) {
        var geopiece = {
            id: geom.GeometryLabel,
            layerid: geom.StyleLabel,
            mapid: geom.MapLabel ? geom.MapLabel:0,
            Positions: mesh.Positions,
            Normals: mesh.Normals,
            Indices: mesh.Indices,
            data: { prod: label }
        };

        var valid = true, plen = geopiece.Positions.length / 3;
        for (var j = 0; j < geopiece.Indices.length; j++) {
            if (geopiece.Indices[j] > 256 * 256 || geopiece.Indices[j] > plen) {
                valid = false;
                break;
            }
        }
        if (valid) {
            eventmanager.FireEvent("Geometry", geopiece);
        } else {
            console.log("invalid geometry piece detected: " + geopiece.id);
        }
    }

    processor.prototype.CreateMatrix = function (trans) {
        if (trans) {
            trans = mat4.create([
                trans.M11,
                trans.M12,
                trans.M13,
                trans.M14,
                trans.M21,
                trans.M22,
                trans.M23,
                trans.M24,
                trans.M31,
                trans.M32,
                trans.M33,
                trans.M34,
                trans.OffsetX,
                trans.OffsetY,
                trans.OffsetZ,
                trans.M44
            ]);
        } else {
            trans = mat4.identity();
        }
        return trans;
    }
    return new processor();
});
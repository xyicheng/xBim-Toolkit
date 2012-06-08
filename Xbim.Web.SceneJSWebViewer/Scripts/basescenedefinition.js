var basescene =
{
        "type": "shader",
        "id": "topNode",
        "coreId": "colorTransShader",
        "shaders": [
            {
                "stage": "fragment",
                "code": [
                    "uniform bool   picked;",
                    "uniform vec3   colorTransScale;",

                    "vec3 PickColorFunc(vec3 color) {",
                    "   if(picked) ",
                    "   {",
                    "       return colorTransScale;",
                    "   }",
                    "   return color;",
                    "}"
                ],
                "hooks": {
                    "materialBaseColor": "PickColorFunc"
                }
            }
      ],
        "params": {
            "picked": false,
            "colorTransScale": [0, 0, 1]
        },
        //because scenejs does not currently support ambient lighting, we need to setup lights in all directions. 
        //We do this at differing strengths so that the shading of items looks right
        "nodes": [
        {
            "type": "light",
            "id": "sun",
            "mode": "dir",
            "color": {
                "r": 0.8,
                "g": 0.8,
                "b": 0.8
            },
            "dir": {
                "x": 0,
                "y": -0.5,
                "z": -1
            },
            "diffuse": true,
            "specular": true
        },
        {
            "type": "light",
            "mode": "dir",
            "color": {
                "r": 0.8,
                "g": 0.8,
                "b": 0.8
            },
            "dir": {
                "x": 0,
                "y": 0.5,
                "z": 1
            },
            "diffuse": true,
            "specular": true
        },
        {
            "type": "light",
            "mode": "dir",
            "color": {
                "r": 0.4,
                "g": 0.4,
                "b": 0.4
            },
            "dir": {
                "x": 1,
                "y": 0,
                "z": 0
            },
            "diffuse": true,
            "specular": true
        },
        {
            "type": "light",
            "mode": "dir",
            "color": {
                "r": 0.4,
                "g": 0.4,
                "b": 0.4
            },
            "dir": {
                "x": -1,
                "y": 0,
                "z": 0
            },
            "diffuse": true,
            "specular": true
        },
        {
            "type": "lookAt",
            "id": "zoom",
            "eye": { "x": 0, "y": 0, "z": -100 },
            "look": { "x": 0, "y": 0, "z": 0 },
            "up": { "y": 1 },
            "nodes": [
            {
                "type": "camera",
                "id": "camera",
                "optics": {
                    "type": "perspective",
                    "fovy": 65,
                    "aspect": 1.5,
                    "near": 0.01,
                    "far": 3000
                },
                "nodes": [
                {
                    "type": "renderer",
                    "clearColor": {
                        "r": 1.0,
                        "g": 1.0,
                        "b": 1.0
                    },
                    "clear": {
                        "depth": true,
                        "color": true
                    },
                    "nodes": [
                    {
                        "type": "scale",
                        "id": "scale",
                        "x": 1,
                        "y": 1,
                        "z": 1,
                        "nodes": [
                        {
                            "type": "flags",
                            "flags": {
                                "transparent": true, //general transparency flag to allow transparent materials like glass
                                "backfaces": true
                            },
                            "nodes": [
                            {
                             
                                    "type": "translate",
                                    "id": "plus",
                                    "x": 0,
                                    "y": 0,
                                    "z": 0,
                                    "nodes": [
                                    {
                                        "type": "translate",
                                        "id": "minus",
                                        "nodes": [
                                        {
                                            "type": "rotate",
                                            "id": "rotX",
                                            "x": 1,
                                            "y": 0,
                                            "z": 0,
                                            "angle": 0,
                                            "nodes": [
                                            {
                                                "type": "rotate",
                                                "id": "rotY",
                                                "x": 0,
                                                "y": 1,
                                                "z": 0,
                                                "angle": 0,
                                                "nodes": [
                                                {
                                                    "type": "rotate",
                                                    "id": "rotZ",
                                                    "x": 0,
                                                    "y": 0,
                                                    "z": 1,
                                                    "angle": 0,
                                                    "nodes": [
                                                    {
                                                        "type": "translate",
                                                        "id": "move",
                                                        "x": 0,
                                                        "y": 0,
                                                        "z": 0,
                                                        "nodes": [
                                                        {
                                                            "type": "matrix",
                                                            "id": "offset"
                                                        }
                                                      ]
                                                    }
                                                  ]
                                                }
                                              ]
                                            }
                                          ]
                                        }
                                      ]
                                    }
                                  ]
                                }
                              ]
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
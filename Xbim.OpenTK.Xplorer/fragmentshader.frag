	varying vec3 Lighting;
    varying vec4 FragColour;

    void main(void) {
        vec4 Colour = FragColour;
        gl_FragColor = vec4(Colour.rgb * Lighting, Colour.a);
    }
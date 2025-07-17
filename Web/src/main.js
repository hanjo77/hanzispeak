import * as THREE from 'three';
import { FBXLoader } from 'three/examples/jsm/loaders/FBXLoader.js';

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.1, 1000);
camera.position.z = 1.5;

const renderer = new THREE.WebGLRenderer({ alpha: true });
renderer.setSize(window.innerWidth, window.innerHeight);

const light = new THREE.HemisphereLight(0xffffff, 0x444444, 1);
scene.add(light);

let currentScale = .0001;

const loader = new FBXLoader();
loader.load('hanzi.fbx', function (fbx) {
  fbx.scale.set(currentScale, currentScale, currentScale);
  fbx.rotation.x = 90;
  fbx.position.set(-.5, -.5, -.5);
  scene.add(fbx);

  function animate() {
    requestAnimationFrame(animate);
    // currentScale += 0.005;
    fbx.scale.set(currentScale, currentScale, currentScale);
    renderer.render(scene, camera);
  }
  animate();
});

window.addEventListener('load', () => {
    document.body.appendChild(renderer.domElement);
})

window.addEventListener('resize', () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

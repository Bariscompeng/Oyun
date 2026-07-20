import json
import uuid

def add_binding(bindings, action, path):
    # Check if already exists
    for b in bindings:
        if b.get('action') == action and b.get('path') == path:
            return
    bindings.append({
        "name": "",
        "id": str(uuid.uuid4()),
        "path": path,
        "interactions": "",
        "processors": "",
        "groups": "",
        "action": action,
        "isComposite": False,
        "isPartOfComposite": False
    })

with open("Assets/Settings/DrivingControls.inputactions", "r", encoding="utf-8") as f:
    data = json.load(f)

# The file contains action maps in data["maps"] typically? Let's check structure.
# Unity inputactions schema: {"maps": [ {"name": "Driving", "actions": [...], "bindings": [...]} ] }
for m in data.get("maps", []):
    if m.get("name") == "Driving":
        bindings = m.get("bindings", [])
        
        # Throttle
        add_binding(bindings, "Throttle", "<Gamepad>/rightTrigger")
        # For Logitech wheels, pedals often map to joystick axes or specific gamepad axes.
        add_binding(bindings, "Throttle", "<Joystick>/stick/up")
        
        # Brake
        add_binding(bindings, "Brake", "<Gamepad>/leftTrigger")
        add_binding(bindings, "Brake", "<Joystick>/stick/down")
        
        # Steer
        add_binding(bindings, "Steer", "<Gamepad>/leftStick/x")
        add_binding(bindings, "Steer", "<Joystick>/stick/x")

with open("Assets/Settings/DrivingControls.inputactions", "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4)

print("Bindings added successfully.")

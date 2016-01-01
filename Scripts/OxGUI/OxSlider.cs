using UnityEngine;
using System;
using System.Collections;

class OxSlider : OxGUI
{
    public float value { get; private set; }
    private float oldValue;
    public bool horizontal = true;
    public float thumbPercentSize = 0.2f, smooth = 0.005f;
    private OxButton sliderThumb, sliderBack;
    private bool dragging;
    private Vector3 mousePosition;
    public event ValueChangedEventHandler valueChanged;

    public delegate void ValueChangedEventHandler(OxGUI sender, float amount);

    public OxSlider(float value)
        : base(Vector2.zero, Vector2.zero)
    {
        this.value = value;
        sliderThumb = new OxButton("");
        sliderThumb.pressed += button_pressed;
        sliderThumb.released += button_released;
        sliderBack = new OxButton("");
        sliderBack.clicked += button_clicked;
    }

    public OxSlider() : this(0f) { }

    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            if (dragging)
            {
                //float prevValue = value;
                if (horizontal) value += (Input.mousePosition.x - mousePosition.x) * smooth;
                else value += (Input.mousePosition.y - mousePosition.y) * smooth;
                mousePosition = Input.mousePosition;
                //if (prevValue != value && valueChanged != null) valueChanged(this, value - prevValue);
            }

            if (value > 1) value = 1;
            if (value < 0) value = 0;
            if (thumbPercentSize > 1) thumbPercentSize = 1;
            if (thumbPercentSize < 0) thumbPercentSize = 0;

            float thumbWidth = size.x * thumbPercentSize, thumbHeight = size.y, thumbPositionX = position.x + (value * (size.x - thumbWidth)), thumbPositionY = position.y;
            if (!horizontal)
            {
                thumbWidth = size.x;
                thumbHeight = size.y * thumbPercentSize;
                thumbPositionX = position.x;
                thumbPositionY = position.y + (value * (size.y - thumbHeight));
            }

            //text = (((int) (Mathf.Pow(10, 2) * value)) / Mathf.Pow(10, 2)).ToString();

            sliderThumb.SetSize(thumbWidth, thumbHeight);
            sliderThumb.Reposition(thumbPositionX, thumbPositionY);

            sliderBack.SetSize(size.x, size.y);
            sliderBack.Reposition(position.x, position.y);

            sliderThumb.text = text;

            sliderBack.Draw();
            sliderThumb.Draw();
        }
    }

    public bool SetValue(float amount)
    {
        if (!dragging)
        {
            if (amount >= 0 && amount <= 1)
            {
                value = amount;
                return true;
            }
        }

        return false;
    }

    void button_pressed(OxGUI sender)
    {
        oldValue = value;
        mousePosition = Input.mousePosition;
        dragging = true;
    }
    void button_released(OxGUI sender)
    {
        dragging = false;
        if (oldValue != value && valueChanged != null) valueChanged(this, value - oldValue);
    }

    void button_clicked(OxGUI sender)
    {
        if (!dragging)
        {
            if (new Rect(position.x, position.y, size.x, size.y).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
            {
                oldValue = value;
                if (horizontal)
                {
                    //Debug.Log((position.x + size.x) + " - " + mousePosition.x);
                    value = 1 - (((position.x + size.x) - Input.mousePosition.x) / size.x);
                    //Debug.Log("New Value: " + value);
                }
                else
                {
                    value = 1 - (((position.y + size.y) - (Screen.height - Input.mousePosition.y)) / size.y);
                }
                if (oldValue != value && valueChanged != null) valueChanged(this, value - oldValue);
            }
        }
    }
}

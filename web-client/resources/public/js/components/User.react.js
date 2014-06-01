/** @jsx React.DOM */ 

var React = require('react');
var ReactPropTypes = React.PropTypes;

var GoogleLoginButton = require('./GoogleLoginButton.react');

var User = React.createClass({
    render: function() {
        if(this.props.user.loggedIn) {
            return (
            <div>
                <img src={this.props.user.userImage} />
            </div>
            );
        } else {   
            return (
                <div>
                    {this.props.user.loggedIn}
                    <GoogleLoginButton />
                </div>
            );
        }
    },
});

module.exports = User;
